namespace PofilesReader
{
    public interface ILocalizedStringManager
    {
        string GetLocalizedString(string scope, string text, bool plural = false, int? index = null);
    }
}