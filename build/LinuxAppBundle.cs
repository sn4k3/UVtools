using System.Security;
using System.Text.RegularExpressions;
using Nuke.Common.Tooling;

namespace build;

public static class LinuxAppBundle
{
    private const string FlatpakManifest = """
                                           # your app's id which ideally should match the manifest file name
                                           app-id: {0}
                                           runtime: org.freedesktop.Platform
                                           runtime-version: "23.08" # version of the runtime
                                           sdk: org.freedesktop.Sdk # the sdk to use
                                           command: {1} # the command to run, basically the command that starts the app
                                           finish-args:
                                             # Flatpaks run in sandbox mode. This is the list of permissions we need
                                             # As we're running an Avalonia App, we need to specify that we want access
                                             # to the window infrastructure of the host
                                             # X11 + XShm access
                                             - --socket=x11
                                             - --share=ipc
                                             # Wayland access
                                             #- --socket=wayland
                                             # GPU acceleration if needed
                                             - --device=dri
                                             # Needs to talk to the network:
                                             - --share=network
                                             # for more information visit https://docs.flatpak.org/en/latest/sandbox-permissions.html#sandbox-permissions

                                           modules:
                                             # This is our app and the ;build' instructions for it
                                             - name: {1}
                                               buildsystem: simple
                                               # In our case we've already run `dotnet publish` so we just need to copy the
                                               # outputs to the right place
                                               build-commands:
                                                 - mkdir -p /app/bin
                                                 - mv ./app-sources /app/bin/app-sources
                                                 # allow our app to be executed
                                                 - chmod +x /app/bin/app-sources/{1}
                                                 # create a symlink to the executable
                                                 - ln -s /app/bin/app-sources/{1} /app/bin/{1}
                                               # This is the list of files/directories we want to copy to the flatpak
                                               # for more elaborated builds we could fetch a zip file, git repository or
                                               # even build from source however that comes with different challenges
                                               # so we'll leave that for another time
                                               sources:
                                                 - type: dir
                                                   # This is the path on the host machine './flatpak/{1}'
                                                   # but since we're running the flatpak-builder command in the flatpak directory
                                                   # we'll omit the other one
                                                   path: {1}
                                                   # This is the path inside the flatpak which gets copied to the root of the flatpak
                                                   dest: app-sources
                                           """;

    public static string GetFlatpakManifest(string appId, string softwareName) =>
        string.Format(FlatpakManifest, appId, softwareName);

    public const string AppImageGitHubUrl = "https://github.com/AppImage/AppImageKit";

    public static string GetAppImageDesktopFile(Build build) =>
        $"""
         [Desktop Entry]
         Type=Application

         Name={build.SoftwareName}
         Comment={build.SoftwareSummary.ReplaceLineEndings(";")}
         Categories=Utility;
         Keywords={build.SoftwarePackageTags}

         Icon={build.SoftwareName}
         Exec={build.SoftwareName}
         Terminal=false
         """;

    public static string GetAppImageAppRunFile(string softwareName) =>
        $$"""
          #!/bin/bash

          # The purpose of this custom AppRun script is to enablesymlinking the AppImage and invoking the corresponding
          # binary depending on which symlink name was used to invoke the AppImage.
          #
          # It also provides some additional help parameters in order to allow faster familiarization with functionality
          # embedded in this AppImage.

          HERE="$(dirname "$(readlink -f "${0}")")"
          export PATH="${HERE}/usr/bin/":"${PATH}"

          function help() {
          echo '  _   ___     ___              _     '
          echo ' | | | \ \   / / |_ ___   ___ | |___ '
          echo ' | | | |\ \ / /| __/ _ \ / _ \| / __|'
          echo ' | |_| | \ V / | || (_) | (_) | \__ \'
          echo '  \___/   \_/   \__\___/ \___/|_|___/'
          echo "
           --------------------------------------------------------------------------
              All the great UVtools functionality inside an AppImage package.
           --------------------------------------------------------------------------
           (This package uses the AppImage software packaging technology for Linux
            ['One App == One File'] for easy availability of the newest UVtools
            releases across all major Linux distributions.)
           Usage:  --help, -h
           ------     # This message
                   <path/to/file1> [path/to/file2] [path/to/file3] [...]
                      # Opens and loads specific file(s) with UVtools
                   --cmd-help
                      # Display UVtoolsCmd help message
                   --cmd, -c <argument(s)> [option(s)]
                      # Redirect a command to UVtoolsCmd
                   --appimage-extract
                      # Unpack this AppImage into a local sub-directory [currently named 'squashfs-root']
                   --appimage-help
                      # Show available AppImage options
          "
          }

          if [ "$1" == "--help" -o "$1" == "-h" ]; then
              help
              exit $?
          fi

          if [ "$1" == "--cmd-help" ]; then
              exec 'UVtoolsCmd' --help
              exit $?
          fi

          if [ "$1" == "--cmd" -o "$1" == "-c" ]; then
          	if [ "$#" -lt 2 ]; then
          		echo 'UVtoolsCmd requires at least one parameter'
          		exec 'UVtoolsCmd' --help
          		exit $?
          	fi

              shift
          	exec 'UVtoolsCmd' "$@"
              exit $?
          fi

          exec '{{softwareName}}' "$@"
          """;

    public static string GetAppImageAppDataXmlFile(Build build)
    {
        var summary = Regex.Replace(build.SoftwareSummary, @"\r\n?|\n", " ", RegexOptions.Multiline)
            .Replace('.', ';');

        if (summary.Length > 78)
        {
            summary = summary[..78];
        }

        return $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <component type="desktop-application">
                  <id>{EscapeXml(build.SoftwareRDNS)}</id>

                  <name>{EscapeXml(build.SoftwareName)}</name>
                  <metadata_license>FSFAP</metadata_license>
                  <project_license>{EscapeXml(build.SoftwareLicense)}</project_license>
                  <content_rating type="oars-1.0" />
                  <summary>{EscapeXml(summary)}</summary>

                  <description>
                    <p>{EscapeXml(build.SoftwareDescription)}</p>
                  </description>

                  <categories>
                    <category>Utility</category>
                  </categories>

                  <supports>
                    <control>pointing</control>
                    <control>keyboard</control>
                    <control>touch</control>
                  </supports>

                  <recommends>
                    <display_length compare="ge">760</display_length>
                  </recommends>

                  <launchable type="desktop-id">{EscapeXml(build.SoftwareRDNS)}.desktop</launchable>
                  
                  <screenshots>
                    <screenshot type="default">
                      <caption>The options dialog</caption>
                      <image>https://raw.githubusercontent.com/sn4k3/UVtools/master/wiki/UI1.png</image>
                    </screenshot>
                  </screenshots>

                  <url type="homepage">{EscapeXml(build.SoftwareRepositoryUrl)}</url>
                  <developer_name>{EscapeXml(build.SoftwareAuthors)}</developer_name>
                  <update_contact>tiago_caza_AT_hotmail.com</update_contact>

                  <provides>
                    <binary>{EscapeXml(build.SoftwareName)}</binary>
                  </provides>

                </component>
                """;
    }

    private static string EscapeXml(string value) => SecurityElement.Escape(value) ?? string.Empty;

    public static bool IsFuseAvailable()
    {
        using var result = ProcessTasks.StartShell("ldconfig -p | grep libfuse.so.2", timeout: 3000);
        result.WaitForExit();
        return result.ExitCode == 0;
    }
}
