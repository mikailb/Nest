namespace Nest.Utilities //This file is to seperate file functions such as removing and checking .png-files in the folder.
                                //makes it easier for both testing and readability.
{
    public static class FileUtil
    {
        public static Func<string, bool> FileExists = System.IO.File.Exists;
        public static Action<string> FileDelete = System.IO.File.Delete;
    }
}
