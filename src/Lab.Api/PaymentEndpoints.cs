using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Lab.Api;

public sealed record CreatePaymentRequest(long AmountCents, string Currency);

public sealed record PaymentResponse(long Id, long AmountCents, string Currency);

public static class PaymentEndpoints
{
    private const int MaxKeyLength = 64;

    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments", CreateAsync);
        app.MapGet("/api/payments/{id:long}", GetAsync);
    }

    private static async Task<IResult> CreateAsync(
        CreatePaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key,
        NpgsqlDataSource db,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > MaxKeyLength)
            return Results.BadRequest(new { error = $"Idempotency-Key header (1-{MaxKeyLength} chars) is required." });
        if (request.AmountCents <= 0)
            return Results.BadRequest(new { error = "amountCents must be greater than 0." });
        if (request.Currency is not { Length: 3 } c || !c.All(char.IsAsciiLetterUpper))
            return Results.BadRequest(new { error = "currency must be a 3-letter ISO 4217 code, e.g. EUR." });

        await using (
            var insert = db.CreateCommand(
                """
                INSERT INTO payments (idempotency_key, amount_cents, currency)
                VALUES (@key, @amount, @currency)
                ON CONFLICT (idempotency_key) DO NOTHING
                RETURNING id
                """
            )
        )
        {
            insert.Parameters.AddWithValue("key", key);
            insert.Parameters.AddWithValue("amount", request.AmountCents);
            insert.Parameters.AddWithValue("currency", request.Currency);

            if (await insert.ExecuteScalarAsync(ct) is long newId)
                return Results.Created(
                    $"/api/payments/{newId}",
                    new PaymentResponse(newId, request.AmountCents, request.Currency)
                );
        }

        var existing =
            await FindByKeyAsync(db, key, ct)
            ?? throw new InvalidOperationException("Payment missing after key conflict.");

        return existing.AmountCents == request.AmountCents && existing.Currency == request.Currency
            ? Results.Ok(existing)
            : Results.Conflict(new { error = "Idempotency-Key was already used with a different request." });
    }

    private static async Task<IResult> GetAsync(long id, NpgsqlDataSource db, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand("SELECT id, amount_cents, currency FROM payments WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return await ReadSingleAsync(cmd, ct) is { } payment ? Results.Ok(payment) : Results.NotFound();
    }

    private static async Task<PaymentResponse?> FindByKeyAsync(NpgsqlDataSource db, string key, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand(
            "SELECT id, amount_cents, currency FROM payments WHERE idempotency_key = @key"
        );
        cmd.Parameters.AddWithValue("key", key);
        return await ReadSingleAsync(cmd, ct);
    }

    private static async Task<PaymentResponse?> ReadSingleAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new PaymentResponse(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2))
            : null;
    }
}
