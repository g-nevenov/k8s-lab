SELECT format('CREATE ROLE api_app LOGIN PASSWORD %L', :'api_password')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'api_app') \gexec
SELECT format('CREATE ROLE audit_checker LOGIN PASSWORD %L', :'checker_password')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'audit_checker') \gexec

CREATE TABLE IF NOT EXISTS payments (
    id               bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    idempotency_key  varchar(64) NOT NULL UNIQUE,
    amount_cents     bigint      NOT NULL CHECK (amount_cents > 0),
    currency         char(3)     NOT NULL,
    created_at       timestamptz NOT NULL DEFAULT now()
);

GRANT SELECT, INSERT ON payments TO api_app;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO api_app;
