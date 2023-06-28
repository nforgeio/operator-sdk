using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace EmptyTask
{
    public class TestTask : Task
    {
        [Required]
        public string InputText { get; set; }
        public override bool Execute()
        {
            //CustomResourceDefinitions = new ITaskItem[InputDlls.Length];

            //for (int i = 0; i < InputDlls.Length; i++)
            //{
            //    Log.LogWarning(InputDlls[i].ItemSpec);
            //    CustomResourceDefinitions[i] = InputDlls[i];
            //}

            Log.LogWarning(InputText);

            return !Log.HasLoggedErrors;
        }
    }
}
