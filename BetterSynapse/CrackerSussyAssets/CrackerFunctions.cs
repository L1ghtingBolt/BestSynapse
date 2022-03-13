using System.Windows;

namespace SynapseX.CrackerSussyAssets
{
    public static class Messages
    {
        public static bool ShowGenericWarningMessage(string message = "This action is irreversible, do it anyways?")
        {
            var warning = MessageBox.Show(
                message,
                "Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            return warning == MessageBoxResult.Yes;
        }

        public static bool ShowGenericQuestionMessage(string message = "This is a question.")
        {
            var question = MessageBox.Show(
                message,
                "Question",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            return question == MessageBoxResult.Yes;
        }

        public static void ShowGenericErrorMessage(string message = "An error has occured!")
        {
            MessageBox.Show(
                message,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    public struct FilterInstance
    {
        public string Title;
        public string Filter;
        public bool IncludeFilter;

        public static string ToString(FilterInstance[] f)
        {
            var total = "";

            for (var i = 0; i < f.Length; i++)
            {
                var filter = f[i];

                var str = filter.Title;
                if (filter.IncludeFilter) str += $" ({filter.Filter})";
                str += $"|{filter.Filter}";
                if (i < f.Length - 1) str += '|';

                total += str;
            }

            return total;
        }
    }
}
