# Signing for the `Papst.EventStore`

Adds cryptographic signing of events to any `Papst.EventStore` implementation. Signing is a handler in
the store's **append pipeline**, so it works uniformly across Azure Cosmos, EF Core, MongoDB,
FileSystem and InMemory without any store-specific code.

Each appended document is signed with a configurable X.509 certificate. Signatures form a
**tamper-evident hash chain** — `sig_N = Sign(H(event_N) ‖ sig_{N-1})` — so payload edits, deletion,
reordering and truncation are all detectable.

## Usage

```csharp
services
    .AddInMemoryEventStore()            // or any other store
    .AddEventRegistrationTypeProvider()
    .AddEventStoreSigning(options =>
    {
        options.Certificate = signingCertificate;   // must contain a private key
        options.Algorithm = "RS256";                // RS/PS/ES + 256/384/512
        options.EnableChaining = true;              // default
    });
```

The private key always comes from the host (certificate store, PFX loaded by the app, Key Vault, …) —
never from configuration.

### Verifying a stream

```csharp
var verifier = provider.GetRequiredService<IEventStreamSignatureVerifier>();
var stream = await eventStore.GetAsync(streamId, ct);

SignatureVerificationResult result = await verifier.VerifyAsync(stream, ct);
if (!result.IsValid)
{
    // result.FirstInvalidVersion, result.Failures
}
```

Set `options.VerifyOnRead = true` to verify every document as it is read (throws
`EventStreamSignatureException` on mismatch).

### Signed entities

Implement `ISignedEntity` (which extends `IEntity`) and the aggregator populates `Signature` with the
signature of the latest applied document, so the aggregated entity carries the signature of its current
version.

## Notes

- The certificate thumbprint and algorithm are stored on the stream index; the current chain head is
  stored there too.
- Verification resolves the historical certificate by thumbprint (`options.CertificateResolver`) so
  certificate rotation does not invalidate previously signed events.
- Signing detects tampering within a retained stream; it does not prevent deletion of a whole stream.
