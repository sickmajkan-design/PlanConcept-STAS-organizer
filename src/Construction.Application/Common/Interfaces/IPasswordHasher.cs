namespace Construction.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);

    /// <summary>
    /// A real hash of a value nobody knows, for verifying against when no
    /// account matched.
    ///
    /// <para>
    /// Sign-in must cost the same whether or not the email exists. Skipping
    /// the hash for an unknown address made those requests measurably faster
    /// — 13x, enough to enumerate the whole staff directory — so the handler
    /// verifies against this instead of short-circuiting.
    /// </para>
    /// </summary>
    string DummyHash { get; }
}
