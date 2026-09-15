#!/usr/bin/env bash
set -uo pipefail

APPLICATION_NAME='UVtools'
APPLICATION_SLUG='uvtools'
EXECUTABLE_NAME='UVtools'
LINUX_APPLICATION_ID='pt.ptrtech.UVtools'
SNAP_NAME='uvtools'
MACOS_BUNDLE_IDENTIFIER='pt.ptrtech.UVtools'
PACKAGE_TYPES=(
  'linux-deb'
  'linux-rpm'
  'linux-arch'
  'linux-appimage'
  'linux-flatpak'
  'linux-snap'
  'macos-app-bundle'
  'macos-pkg'
  'macos-dmg'
  'dotnet-single-file'
  'portable'
)

show_help() {
  local script_name="${0##*/}"
  printf 'Usage:\n'
  printf '  %s\n' "$script_name"
  printf '  %s --portable [PATH]\n' "$script_name"
  printf '  %s help\n\n' "$script_name"
  printf 'Removes every detected installation represented by the generated package formats.\n'
  printf 'Use --portable to also remove PATH/%s (default: current directory).\n' "$APPLICATION_NAME"
}

command_exists() {
  command -v "$1" >/dev/null 2>&1
}

run_elevated() {
  if [ "$(id -u)" -eq 0 ]; then
    "$@"
  elif command_exists sudo; then
    sudo "$@"
  else
    printf 'Warning: removing %s requires root privileges or sudo.\n' "$APPLICATION_NAME" >&2
    return 1
  fi
}

mark_removed() {
  REMOVED_ANY='true'
  printf 'Removed %s.\n' "$1"
}

mark_failed() {
  FAILED_ANY='true'
  printf 'Warning: could not remove %s.\n' "$1" >&2
}

remove_path() {
  local path="$1"
  local description="$2"
  if [ -e "$path" ] || [ -L "$path" ]; then
    if rm -rf -- "$path"; then
      mark_removed "$description"
    else
      mark_failed "$description"
    fi
  fi
}

remove_elevated_path() {
  local path="$1"
  local description="$2"
  if [ -e "$path" ] || [ -L "$path" ]; then
    if run_elevated rm -rf -- "$path"; then
      mark_removed "$description"
    else
      mark_failed "$description"
    fi
  fi
}

uninstall_portable() {
  remove_path "${XDG_DATA_HOME:-$HOME/.local/share}/${APPLICATION_SLUG}" \
    'Portable installation'
  remove_path "$HOME/.local/bin/$EXECUTABLE_NAME" 'Portable launcher'
  if [ -n "$PORTABLE_PARENT" ]; then
    remove_path "${PORTABLE_PARENT%/}/${APPLICATION_NAME}" 'custom Portable extraction'
  fi
}

uninstall_single_file() {
  remove_path "$HOME/.local/bin/${APPLICATION_NAME}.bin" '.NET single-file installation'
  if [ "$EXECUTABLE_NAME" != "${APPLICATION_NAME}.bin" ]; then
    remove_path "$HOME/.local/bin/$EXECUTABLE_NAME" 'legacy .NET single-file installation'
  fi
}

uninstall_appimage() {
  remove_path "$HOME/Applications/${APPLICATION_NAME}.AppImage" 'AppImage installation'
  if [ "$APPLICATION_SLUG" != "$APPLICATION_NAME" ]; then
    remove_path "$HOME/Applications/${APPLICATION_SLUG}.AppImage" 'legacy AppImage installation'
  fi
}

uninstall_flatpak() {
  command_exists flatpak || return
  if flatpak info --user "$LINUX_APPLICATION_ID" >/dev/null 2>&1; then
    if flatpak uninstall --user --noninteractive -y "$LINUX_APPLICATION_ID"; then
      mark_removed 'user Flatpak installation'
    else
      mark_failed 'user Flatpak installation'
    fi
  fi
  if flatpak info --system "$LINUX_APPLICATION_ID" >/dev/null 2>&1; then
    if run_elevated flatpak uninstall --system --noninteractive -y "$LINUX_APPLICATION_ID"; then
      mark_removed 'system Flatpak installation'
    else
      mark_failed 'system Flatpak installation'
    fi
  fi
}

uninstall_snap() {
  command_exists snap || return
  if snap list "$SNAP_NAME" >/dev/null 2>&1; then
    if run_elevated snap remove "$SNAP_NAME"; then
      mark_removed 'Snap installation'
    else
      mark_failed 'Snap installation'
    fi
  fi
}

uninstall_deb() {
  command_exists dpkg-query || return
  if dpkg-query -W -f='${Status}' "$APPLICATION_SLUG" 2>/dev/null |
    grep -q 'install ok installed'; then
    if command_exists apt-get; then
      run_elevated apt-get remove --yes "$APPLICATION_SLUG"
    else
      run_elevated dpkg --remove "$APPLICATION_SLUG"
    fi && mark_removed 'Debian package' || mark_failed 'Debian package'
  fi
}

uninstall_rpm() {
  command_exists rpm || return
  if rpm -q "$APPLICATION_SLUG" >/dev/null 2>&1; then
    if command_exists dnf5; then
      run_elevated dnf5 remove --assumeyes "$APPLICATION_SLUG"
    elif command_exists dnf; then
      run_elevated dnf remove --assumeyes "$APPLICATION_SLUG"
    elif command_exists yum; then
      run_elevated yum remove --assumeyes "$APPLICATION_SLUG"
    elif command_exists zypper; then
      run_elevated zypper --non-interactive remove "$APPLICATION_SLUG"
    else
      run_elevated rpm --erase "$APPLICATION_SLUG"
    fi && mark_removed 'RPM package' || mark_failed 'RPM package'
  fi
}

uninstall_arch() {
  command_exists pacman || return
  if pacman -Q "$APPLICATION_SLUG" >/dev/null 2>&1; then
    if run_elevated pacman -R --noconfirm "$APPLICATION_SLUG"; then
      mark_removed 'Arch Linux package'
    else
      mark_failed 'Arch Linux package'
    fi
  fi
}

uninstall_macos_application() {
  remove_elevated_path "/Applications/${APPLICATION_NAME}.app" 'macOS application'
}

forget_macos_package() {
  command_exists pkgutil || return
  if pkgutil --pkg-info "$MACOS_BUNDLE_IDENTIFIER" >/dev/null 2>&1; then
    if run_elevated pkgutil --forget "$MACOS_BUNDLE_IDENTIFIER" >/dev/null; then
      mark_removed 'macOS package receipt'
    else
      mark_failed 'macOS package receipt'
    fi
  fi
}

uninstall_package() {
  case "$1" in
    portable) [ "$PLATFORM" = 'linux' ] && uninstall_portable ;;
    dotnet-single-file) [ "$PLATFORM" = 'linux' ] && uninstall_single_file ;;
    linux-appimage) [ "$PLATFORM" = 'linux' ] && uninstall_appimage ;;
    linux-flatpak) [ "$PLATFORM" = 'linux' ] && uninstall_flatpak ;;
    linux-snap) [ "$PLATFORM" = 'linux' ] && uninstall_snap ;;
    linux-deb) [ "$PLATFORM" = 'linux' ] && uninstall_deb ;;
    linux-rpm) [ "$PLATFORM" = 'linux' ] && uninstall_rpm ;;
    linux-arch) [ "$PLATFORM" = 'linux' ] && uninstall_arch ;;
    macos-app-bundle|macos-dmg) [ "$PLATFORM" = 'macos' ] && uninstall_macos_application ;;
    macos-pkg)
      if [ "$PLATFORM" = 'macos' ]; then
        uninstall_macos_application
        forget_macos_package
      fi
      ;;
  esac
}

PORTABLE_PARENT=''
case "$#" in
  0) ;;
  1)
    case "$1" in
      help|-h|--help|/help|'/?') show_help; exit 0 ;;
      --portable) PORTABLE_PARENT="$PWD" ;;
      --portable=*) PORTABLE_PARENT="${1#*=}" ;;
      *) printf 'Error: unknown option: %s\n' "$1" >&2; exit 1 ;;
    esac
    ;;
  2)
    [ "$1" = '--portable' ] || { printf 'Error: unknown option: %s\n' "$1" >&2; exit 1; }
    PORTABLE_PARENT="$2"
    ;;
  *) printf 'Error: too many arguments.\n' >&2; exit 1 ;;
esac
if [ "$#" -gt 0 ] && [ -z "$PORTABLE_PARENT" ]; then
  printf 'Error: Portable destination path cannot be empty.\n' >&2
  exit 1
fi

case "$(uname -s)" in
  Linux) PLATFORM='linux' ;;
  Darwin) PLATFORM='macos' ;;
  *) printf 'Error: unsupported operating system: %s\n' "$(uname -s)" >&2; exit 1 ;;
esac

REMOVED_ANY='false'
FAILED_ANY='false'
for package_type in "${PACKAGE_TYPES[@]}"; do
  uninstall_package "$package_type"
done

if [ "$REMOVED_ANY" = 'false' ]; then
  printf 'No %s installations were found.\n' "$APPLICATION_NAME"
elif [ "$FAILED_ANY" = 'false' ]; then
  printf '%s was uninstalled successfully.\n' "$APPLICATION_NAME"
fi
[ "$FAILED_ANY" = 'false' ]
