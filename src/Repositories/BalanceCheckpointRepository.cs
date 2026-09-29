using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PockitBook.Models;

namespace PockitBook.Repositories;

/// <summary>
/// Handles reading and writing BalanceCheckpoint records, and computing a balance as-of a
/// given date from the most recent checkpoint plus transactions since.
/// </summary>
public class BalanceCheckpointRepository
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public BalanceCheckpointRepository(
        SqliteDatabase database,
        TransactionRepository transactionRepository,
        ILogger<BalanceCheckpointRepository> logger)
    {
        _database = database;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    private readonly SqliteDatabase _database;
    private readonly TransactionRepository _transactionRepository;
    private readonly ILogger<BalanceCheckpointRepository> _logger;

    /// <summary>
    /// Adds a balance checkpoint to the database. Used both for manual overrides and for
    /// checkpoints derived from an import source.
    /// </summary>
    public async Task<int> AddAsync(BalanceCheckpoint checkpoint)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string insertStatement =
            """
                INSERT INTO balance_checkpoints (account_id, date, balance, source, note)
                VALUES (@AccountId, @Date, @Balance, @Source, @Note);
            """;

        try
        {
            return await connection.ExecuteAsync(insertStatement, checkpoint);
        }
        catch (Exception e)
        {
            string serializedModel = JsonSerializer.Serialize(checkpoint);
            _logger.LogError(e, "Failed to insert into `balance_checkpoints`. Model:\n{Model}", serializedModel);
            return 0;
        }
    }

    /// <summary>
    /// Returns the most recent checkpoint on or before the given date, or null if none exists.
    /// </summary>
    public async Task<BalanceCheckpoint?> GetLatestThroughDateAsync(int accountId, DateTime date)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string sqlCommand =
            """
                SELECT
                    id as Id,
                    account_id as AccountId,
                    date as Date,
                    balance as Balance,
                    source as Source,
                    note as Note
                FROM
                    balance_checkpoints
                WHERE
                    account_id = @AccountId AND date <= @Date
                ORDER BY
                    date DESC
                LIMIT 1
            """;

        try
        {
            var results = await connection.QueryAsync<BalanceCheckpoint>(sqlCommand, new { AccountId = accountId, Date = date });
            return results.FirstOrDefault();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to select from `balance_checkpoints` table.");
            return null;
        }
    }

    /// <summary>
    /// Returns all checkpoints on or before the given date, ordered oldest first.
    /// </summary>
    public async Task<List<BalanceCheckpoint>> GetAllThroughDateAsync(int accountId, DateTime date)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string sqlCommand =
            """
                SELECT
                    id as Id,
                    account_id as AccountId,
                    date as Date,
                    balance as Balance,
                    source as Source,
                    note as Note
                FROM
                    balance_checkpoints
                WHERE
                    account_id = @AccountId AND date <= @Date
                ORDER BY
                    date ASC
            """;

        try
        {
            var results = await connection.QueryAsync<BalanceCheckpoint>(sqlCommand, new { AccountId = accountId, Date = date });
            return results.ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to select from `balance_checkpoints` table.");
            return [];
        }
    }

    /// <summary>
    /// Computes the balance for an account as-of a given date: the most recent checkpoint at or
    /// before that date, plus the sum of transactions after that checkpoint through that date.
    /// If there's no checkpoint at all, the balance is just the transaction sum from the
    /// beginning of time (i.e. assumes a starting balance of zero).
    /// </summary>
    public async Task<decimal> GetComputedBalanceAsync(int accountId, DateTime asOfDate)
    {
        BalanceCheckpoint? checkpoint = await GetLatestThroughDateAsync(accountId, asOfDate);
        decimal baseBalance = checkpoint?.Balance ?? 0m;
        DateTime baseDate = checkpoint?.Date ?? DateTime.MinValue;

        decimal transactionSum = await _transactionRepository.GetSumAsync(accountId, baseDate, asOfDate);
        return baseBalance + transactionSum;
    }

    /// <summary>
    /// Builds the "actual" balance history for an account through a given date.
    ///
    /// If there's no checkpoint at all yet, this is just a guess: the walk starts from an
    /// assumed balance of zero at the earliest transaction, since there's no known truth to
    /// anchor from.
    ///
    /// Once at least one checkpoint exists, though, transactions *before* the earliest checkpoint
    /// are reconstructed BACKWARD from it instead - each one is undone in reverse chronological
    /// order to work out what the balance must have been before it occurred. This is what lets a
    /// single override entered today (after importing months of history) correct the entire
    /// historical curve, not just the balance from today onward: the confirmed "truth" propagates
    /// backward through every transaction between it and the start of history.
    ///
    /// Transactions/checkpoints from the earliest checkpoint onward are then walked forward as
    /// usual: a later checkpoint is a hard reset (the running balance snaps to it regardless of
    /// what the transaction math said), which is how a real drift correction between two
    /// checkpoints still shows up as a visible jump.
    /// </summary>
    public async Task<List<LabeledDateTimePoint>> GetBalanceHistoryAsync(int accountId, DateTime throughDate)
    {
        List<Transaction> orderedTransactions = (await _transactionRepository.GetByAccountThroughDateAsync(accountId, throughDate))
            .OrderBy(transaction => transaction.Date)
            .ToList();

        List<BalanceCheckpoint> orderedCheckpoints = await GetAllThroughDateAsync(accountId, throughDate);

        if (orderedCheckpoints.Count == 0)
            return BuildZeroAnchoredHistory(orderedTransactions, throughDate);

        var points = new List<LabeledDateTimePoint>();
        BalanceCheckpoint firstCheckpoint = orderedCheckpoints[0];

        // Transactions strictly before the first checkpoint get reconstructed backward from it.
        List<Transaction> transactionsBeforeFirstCheckpoint = orderedTransactions
            .Where(transaction => transaction.Date < firstCheckpoint.Date)
            .OrderByDescending(transaction => transaction.Date)
            .ToList();

        if (transactionsBeforeFirstCheckpoint.Count > 0)
        {
            decimal cursorBalance = firstCheckpoint.Balance;
            var backwardPoints = new List<LabeledDateTimePoint>();

            foreach (Transaction transaction in transactionsBeforeFirstCheckpoint)
            {
                // cursorBalance currently represents the balance immediately AFTER `transaction`
                // (true for the latest one, since nothing else happens between it and the
                // checkpoint) - record that, then undo it to find the balance before it.
                backwardPoints.Add(new LabeledDateTimePoint(transaction.Date, (double)cursorBalance, transaction.Description, transaction.Amount));
                cursorBalance -= transaction.Amount;
            }

            backwardPoints.Reverse(); // oldest to newest again

            // cursorBalance now holds the reconstructed true starting balance, before the
            // earliest transaction we have - this is the number that answers "it probably
            // doesn't actually start at zero, right?".
            points.Add(new LabeledDateTimePoint(transactionsBeforeFirstCheckpoint[^1].Date, (double)cursorBalance, "Starting balance"));
            points.AddRange(backwardPoints);
        }

        points.Add(new LabeledDateTimePoint(firstCheckpoint.Date, (double)firstCheckpoint.Balance, firstCheckpoint.Note ?? "Balance checkpoint"));

        // From here on, walk forward exactly as before: remaining transactions/checkpoints after
        // the first checkpoint, snapping the balance at each subsequent checkpoint.
        decimal runningBalance = firstCheckpoint.Balance;
        DateTime lastAppliedDate = firstCheckpoint.Date;
        int checkpointIndex = 1;

        void ApplyCheckpointsThrough(DateTime cutoff)
        {
            while (checkpointIndex < orderedCheckpoints.Count && orderedCheckpoints[checkpointIndex].Date <= cutoff)
            {
                BalanceCheckpoint checkpoint = orderedCheckpoints[checkpointIndex];
                runningBalance = checkpoint.Balance;
                lastAppliedDate = checkpoint.Date;
                points.Add(new LabeledDateTimePoint(checkpoint.Date, (double)runningBalance, checkpoint.Note ?? "Balance checkpoint"));
                checkpointIndex++;
            }
        }

        foreach (Transaction transaction in orderedTransactions.Where(t => t.Date > firstCheckpoint.Date))
        {
            if (transaction.Date <= lastAppliedDate)
                continue;

            ApplyCheckpointsThrough(transaction.Date);
            if (transaction.Date <= lastAppliedDate)
                continue; // absorbed by a checkpoint just applied on/after this transaction's date

            runningBalance += transaction.Amount;
            lastAppliedDate = transaction.Date;
            points.Add(new LabeledDateTimePoint(transaction.Date, (double)runningBalance, transaction.Description, transaction.Amount));
        }

        ApplyCheckpointsThrough(throughDate);

        return points;
    }

    private static List<LabeledDateTimePoint> BuildZeroAnchoredHistory(List<Transaction> orderedTransactions, DateTime throughDate)
    {
        if (orderedTransactions.Count == 0)
            return [new LabeledDateTimePoint(throughDate, 0d, "Starting balance")];

        var points = new List<LabeledDateTimePoint> { new(orderedTransactions[0].Date, 0d, "Starting balance") };
        decimal runningBalance = 0m;

        foreach (Transaction transaction in orderedTransactions)
        {
            runningBalance += transaction.Amount;
            points.Add(new LabeledDateTimePoint(transaction.Date, (double)runningBalance, transaction.Description, transaction.Amount));
        }

        return points;
    }
}
