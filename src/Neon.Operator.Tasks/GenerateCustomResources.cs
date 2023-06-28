using System.Linq;

using k8s;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Neon.Operator.Core;
using Neon.Operator.Entities;

namespace Neon.Operator.Tasks
{
    public class GenerateCustomResourceDefinitions : Task
    {
        public GenerateCustomResourceDefinitions()
        {
            System.Diagnostics.Debugger.Launch();
        }

        public const string NameKey = "Name";
        public static CustomResourceGenerator Generator = new CustomResourceGenerator();

        [Required]
        public ITaskItem[] InputDlls { get; set; }

        [Output]
        public ITaskItem[] CustomResourceDefinitions { get; set; }

        public override bool Execute()
        {
            var scanner = new AssemblyScanner();

            for (int i = 0; i < InputDlls.Length; i++)
            {
                scanner.Add(InputDlls[i].ItemSpec);
            }

            CustomResourceDefinitions = new ITaskItem[scanner.EntityTypes.Count];

            for (int i = 0; i < scanner.EntityTypes.Count; i++)
            {
                var crd = Generator.GenerateCustomResourceDefinition(scanner.EntityTypes.ElementAt(i));

                var outputItem = new TaskItem(KubernetesYaml.Serialize(crd));
                outputItem.SetMetadata(NameKey, crd.Metadata.Name);
                CustomResourceDefinitions[i] = outputItem;
            }

            return !Log.HasLoggedErrors;
        }
    }
}
