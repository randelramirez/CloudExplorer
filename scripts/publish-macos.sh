#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_root="$(cd -- "${script_dir}/.." && pwd)"
project_file="${project_root}/CloudExplorer/CloudExplorer.csproj"
artifacts_dir="${project_root}/artifacts"
packages_dir="${project_root}/installer/macOS"

architecture=""
package_format="app"
version=""
codesign_key=""
package_signing_key=""
notary_profile=""
staging_dir=""

usage() {
  printf '%s\n' \
    "Usage: ./scripts/publish-macos.sh [options]" \
    "" \
    "Options:" \
    "  --arch x64|arm64             Target processor architecture (default: current Mac)" \
    "  --format app|dmg|pkg         Output format (default: app zip)" \
    "  --version VERSION            Override ApplicationDisplayVersion" \
    "  --codesign-key IDENTITY      Developer ID Application identity" \
    "  --package-signing-key ID     Developer ID Installer identity (pkg only)" \
    "  --notary-profile PROFILE     notarytool keychain profile" \
    "  --help                       Show this help"
}

cleanup() {
  if [[ -n "${staging_dir}" && "${staging_dir}" == "${artifacts_dir}/.publish-macos."* ]]; then
    rm -rf -- "${staging_dir}"
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --arch)
      architecture="${2:-}"
      shift 2
      ;;
    --format)
      package_format="${2:-}"
      shift 2
      ;;
    --version)
      version="${2:-}"
      shift 2
      ;;
    --codesign-key)
      codesign_key="${2:-}"
      shift 2
      ;;
    --package-signing-key)
      package_signing_key="${2:-}"
      shift 2
      ;;
    --notary-profile)
      notary_profile="${2:-}"
      shift 2
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      printf 'Unknown option: %s\n' "$1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [[ "$(uname -s)" != "Darwin" ]]; then
  printf '%s\n' 'Native macOS bundles must be published on macOS.' >&2
  exit 1
fi

if [[ -z "${architecture}" ]]; then
  case "$(uname -m)" in
    arm64) architecture="arm64" ;;
    x86_64) architecture="x64" ;;
    *) printf 'Unsupported Mac architecture: %s\n' "$(uname -m)" >&2; exit 2 ;;
  esac
fi

if [[ "${architecture}" != "x64" && "${architecture}" != "arm64" ]]; then
  printf 'Unsupported macOS architecture: %s\n' "${architecture}" >&2
  exit 2
fi

if [[ "${package_format}" != "app" && "${package_format}" != "dmg" && "${package_format}" != "pkg" ]]; then
  printf 'Unsupported macOS package format: %s\n' "${package_format}" >&2
  exit 2
fi

if [[ "${package_format}" != "app" && -z "${codesign_key}" ]]; then
  printf '%s\n' '--codesign-key is required for dmg and pkg distribution.' >&2
  exit 2
fi

if [[ "${package_format}" == "pkg" && -z "${package_signing_key}" ]]; then
  printf '%s\n' '--package-signing-key is required for pkg distribution.' >&2
  exit 2
fi

for required_command in dotnet ditto shasum; do
  if ! command -v "${required_command}" >/dev/null 2>&1; then
    printf 'Required command is missing: %s\n' "${required_command}" >&2
    exit 127
  fi
done

if [[ -z "${version}" ]]; then
  version="$(dotnet msbuild "${project_file}" -nologo -getProperty:ApplicationDisplayVersion | tail -n 1 | tr -d '\r')"
fi

if [[ ! "${version}" =~ ^[0-9A-Za-z][0-9A-Za-z.+-]*$ ]]; then
  printf 'Invalid application version: %s\n' "${version}" >&2
  exit 2
fi

mkdir -p "${packages_dir}"
staging_dir="$(mktemp -d "${artifacts_dir}/.publish-macos.XXXXXX")"
trap cleanup EXIT

runtime_identifier="osx-${architecture}"
publish_dir="${staging_dir}/publish"
mkdir -p "${publish_dir}"

publish_arguments=(
  publish "${project_file}"
  --framework net10.0-desktop
  --configuration Release
  --runtime "${runtime_identifier}"
  --self-contained true
  -p:TargetFrameworks=net10.0-desktop
  -p:PackageFormat="${package_format}"
  -p:PublishTrimmed=false
  -p:UnoMacOSIncludeDebugSymbols=false
  -p:ApplicationDisplayVersion="${version}"
  -p:PublishDir="${publish_dir}/"
)

if [[ -n "${codesign_key}" ]]; then
  publish_arguments+=(-p:CodesignKey="${codesign_key}")
fi

if [[ "${package_format}" == "pkg" ]]; then
  publish_arguments+=(-p:PackageSigningKey="${package_signing_key}")
elif [[ "${package_format}" == "dmg" ]]; then
  publish_arguments+=(-p:DiskImageSigningKey="${codesign_key}")
fi

if [[ -n "${notary_profile}" ]]; then
  publish_arguments+=(-p:UnoMacOSNotarizeKeychainProfile="${notary_profile}")
fi

dotnet "${publish_arguments[@]}"

package_base="cloud-explorer-${version}-${runtime_identifier}"
if [[ "${package_format}" == "app" ]]; then
  app_bundle=""
  app_bundle_count=0
  while IFS= read -r candidate; do
    app_bundle="${candidate}"
    app_bundle_count=$((app_bundle_count + 1))
  done < <(find "${publish_dir}" -maxdepth 1 -type d -name '*.app' -print)
  if [[ ${app_bundle_count} -ne 1 ]]; then
    printf 'Expected one app bundle, found %s.\n' "${app_bundle_count}" >&2
    exit 1
  fi

  if [[ -n "${codesign_key}" ]]; then
    codesign --verify --deep --strict "${app_bundle}"
  fi

  archive_path="${packages_dir}/${package_base}.zip"
  rm -f -- "${archive_path}"
  ditto -c -k --sequesterRsrc --keepParent "${app_bundle}" "${archive_path}"
else
  native_package=""
  native_package_count=0
  while IFS= read -r candidate; do
    native_package="${candidate}"
    native_package_count=$((native_package_count + 1))
  done < <(find "${publish_dir}" -maxdepth 1 -type f -name "*.${package_format}" -print)
  if [[ ${native_package_count} -ne 1 ]]; then
    printf 'Expected one %s package, found %s.\n' "${package_format}" "${native_package_count}" >&2
    exit 1
  fi

  archive_path="${packages_dir}/${package_base}.${package_format}"
  cp "${native_package}" "${archive_path}"
fi

checksum_path="${archive_path}.sha256"
(
  cd -- "${packages_dir}"
  shasum -a 256 "$(basename "${archive_path}")" >"$(basename "${checksum_path}")"
)

printf 'Package: %s\nChecksum: %s\n' "${archive_path}" "${checksum_path}"
