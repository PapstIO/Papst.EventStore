using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Serilog;

class Build : NukeBuild
{
  /// Support plugins are available for:
  ///   - JetBrains ReSharper        https://nuke.build/resharper
  ///   - JetBrains Rider            https://nuke.build/rider
  ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
  ///   - Microsoft VSCode           https://nuke.build/vscode
  public static int Main()
  {
    return Execute<Build>(x => x.Compile);
  }

  [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
  readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

  [Parameter("NuGet Api Key"), Secret]
  readonly string NuGetApiSecret;

  [Solution] readonly Solution Solution;
  

  Target Clean => target => target
    .Before(Restore)
    .Executes(() =>
    {
      DotNetTasks.DotNetClean();
    });

  Target Restore => target => target
    .Executes(() =>
    {
      DotNetTasks.DotNetRestore(c => c.SetProjectFile(Solution));
    });

  Target Test => target => target.DependsOn(Restore).DependsOn(Compile).Executes(() =>
  {
    DotNetTasks.DotNetTest(c => c.SetProjectFile(Solution));
  });

  Target Compile => target => target
    .DependsOn(Restore)
    .Executes(() =>
    {
      Log.Information("Building Version {Version}", ThisAssembly.AssemblyInformationalVersion);
      foreach (Project prj in Solution.AllProjects.Where(p => p.Name.Contains("EventStore")))
      {
        Log.Information("Building {Project}", prj.Name);
        DotNetTasks.DotNetBuild(c => c.SetProjectFile(prj).SetConfiguration(Configuration));
      }
    });

  Target Publish => target => target
    .DependsOn(Restore)
    .Requires(() => NuGetApiSecret)
    .Executes(() =>
    {
      Log.Information("Publishing Version {Version}", ThisAssembly.AssemblyInformationalVersion);
      foreach (Project prj in Solution.AllProjects.Where(p => !p.Name.Contains("Test") && !p.Name.Contains("Build")))
      {
        string outputPath = Path.Combine(".", ".build", prj.Name);
        Log.Information("Publishing Project {Project} to NuGet", prj.Name);
        DotNetTasks.DotNetPublish(c => c
          .SetProject(prj)
          .SetOutput(outputPath));
        IReadOnlyCollection<string> files = Globbing.GlobFiles(outputPath, "*.nupkg");
        foreach (string file in files)
        {
          DotNetTasks.DotNetNuGetPush(c => c.SetApiKey(NuGetApiSecret).SetTargetPath(file));
        }
      }
    });
}
