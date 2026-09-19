#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_root="$(cd -- "${script_dir}/.." && pwd)"
project_file="${project_root}/CloudExplorer/CloudExplorer.csproj"
artifacts_dir="${project_root}/artifacts"
packages_dir="${project_root}/installer/ubuntu"

architecture="x64"
package_format="portable"
version=""
staging_dir=""

usage() {
  printf '%s\n' \
    "Usage: ./scripts/publish-ubuntu.sh [options]" \
    "" \
    "Options:" \
    "  --arch x64|arm64          Target processor architecture (default: x64)" \
    "  --format portable|snap    Output format (default: portable)" \
    "  --version VERSION         Override ApplicationDisplayVersion" \
    "  --help                    Show this help"
}

cleanup() {
  if [[ -n "${staging_dir}" && "${staging_dir}" == "${artifacts_dir}/.publish-ubuntu."* ]]; then
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

if [[ "${architecture}" != "x64" && "${architecture}" != "arm64" ]]; then
  printf 'Unsupported Ubuntu architecture: %s\n' "${architecture}" >&2
  exit 2
fi

if [[ "${package_format}" != "portable" && "${package_format}" != "snap" ]]; then
  printf 'Unsupported Ubuntu package format: %s\n' "${package_format}" >&2
  exit 2
fi

if ! command -v dotnet >/dev/null 2>&1; then
  printf '%s\n' '.NET SDK is required on PATH.' >&2
  exit 127
fi

if [[ -z "${version}" ]]; then
  version="$(dotnet msbuild "${project_file}" -nologo -getProperty:ApplicationDisplayVersion | tail -n 1 | tr -d '\r')"
fi

if [[ ! "${version}" =~ ^[0-9A-Za-z][0-9A-Za-z.+-]*$ ]]; then
  printf 'Invalid application version: %s\n' "${version}" >&2
  exit 2
fi

mkdir -p "${packages_dir}"
staging_dir="$(mktemp -d "${artifacts_dir}/.publish-ubuntu.XXXXXX")"
trap cleanup EXIT

runtime_identifier="linux-${architecture}"
package_name="cloud-explorer-${version}-${runtime_identifier}"

if [[ "${package_format}" == "portable" ]]; then
  bundle_dir="${staging_dir}/CloudExplorer"
  archive_path="${packages_dir}/${package_name}.tar.gz"

  dotnet publish "${project_file}" \
    --framework net10.0-desktop \
    --configuration Release \
    --runtime "${runtime_identifier}" \
    --self-contained true \
    -p:TargetFrameworks=net10.0-desktop \
    -p:PublishTrimmed=false \
    -p:ApplicationDisplayVersion="${version}" \
    --output "${bundle_dir}"

  test -x "${bundle_dir}/CloudExplorer"
  find "${bundle_dir}" -type f -name '*.pdb' -delete
  tar -C "${staging_dir}" -czf "${archive_path}" CloudExplorer
else
  if [[ -r /etc/os-release ]]; then
    # shellcheck disable=SC1091
    source /etc/os-release
    case "${VERSION_ID:-}" in
      20.04|22.04|24.04) ;;
      *)
        printf 'Uno Snap packaging does not recognize Ubuntu %s. Use --format portable or build the Snap on Ubuntu 20.04, 22.04, or 24.04.\n' "${VERSION_ID:-unknown}" >&2
        exit 1
        ;;
    esac
  fi

  if ! command -v snapcraft >/dev/null 2>&1; then
    printf '%s\n' 'snapcraft is required for --format snap. See docs/platforms/ubuntu.md.' >&2
    exit 127
  fi

  snap_publish_dir="${staging_dir}/snap"
  mkdir -p "${snap_publish_dir}"

  dotnet publish "${project_file}" \
    --framework net10.0-desktop \
    --configuration Release \
    --runtime "${runtime_identifier}" \
    --self-contained true \
    -p:TargetFrameworks=net10.0-desktop \
    -p:PackageFormat=snap \
    -p:ApplicationDisplayVersion="${version}" \
    -p:PublishDir="${snap_publish_dir}/"

  mapfile -t snap_files < <(find "${snap_publish_dir}" -maxdepth 1 -type f -name '*.snap' -print)
  if [[ ${#snap_files[@]} -ne 1 ]]; then
    printf 'Expected one Snap package, found %s.\n' "${#snap_files[@]}" >&2
    exit 1
  fi

  archive_path="${packages_dir}/$(basename "${snap_files[0]}")"
  cp -- "${snap_files[0]}" "${archive_path}"
fi

checksum_path="${archive_path}.sha256"
(
  cd -- "${packages_dir}"
  sha256sum "$(basename "${archive_path}")" >"$(basename "${checksum_path}")"
)

printf 'Package: %s\nChecksum: %s\n' "${archive_path}" "${checksum_path}"
