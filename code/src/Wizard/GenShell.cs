﻿using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Templates.Wizard
{
    public abstract class GenShell
    {
        public string ProjectName { get; protected set; }
        public string ProjectPath { get; protected set; }
        public string OutputPath { get; protected set; }

        public GenShell()
        {
            ProjectName = GetActiveProjectName();
            ProjectPath = GetActiveProjectPath();
            OutputPath = GetSelectedItemPath();
        }

        public GenShell(Dictionary<string, string> replacements)
        {
            ProjectName = replacements["$safeprojectname$"];

            var di = new DirectoryInfo(replacements["$destinationdirectory$"]);
            ProjectPath = di.FullName;
            OutputPath = di.Parent.FullName;
        }

        protected abstract string GetActiveProjectName();
        protected abstract string GetActiveProjectPath();
        protected abstract string GetSelectedItemPath();

        public abstract bool SetActiveConfigurationAndPlatform(string configurationName, string platformName);
        public abstract void ShowStatusBarMessage(string message);
        public abstract void AddProjectToSolution(string projectFullPath);
        public abstract void AddItemToActiveProject(string itemFullPath);
        public abstract void SaveSolution(string solutionFullPath);
        public abstract string GetActiveNamespace();

        public void CancelWizard()
        {
            throw new WizardCancelledException();
        }
    }
}
