using GalaSoft.MvvmLight;

namespace SearchMSAccess
{
    public class SearchResultModel : ViewModelBase
    {
        public string FileName { get; private set; }
        public string ModuleName { get; private set; }
        public int LineNumber { get; private set; }
        public string LineContent { get; private set; }

        public SearchResultModel(string filename, string modulename, int linenumber, string linecontent)
        {
            FileName = filename;
            ModuleName = modulename;
            LineNumber = linenumber;
            LineContent = linecontent;
        }
    }
}
