"""Check what the repository publishes: no game files, consistent mod metadata, working links.

Building the mod and the offline tests need the game's assemblies, so they run only on a machine
with the game installed. This script needs only Python and Git.
"""

from __future__ import annotations

import json
import os
import re
import subprocess
from dataclasses import dataclass
from pathlib import Path
from urllib.parse import unquote, urlsplit

MOD: str = "TurnBasedPartyAI"
AUTHOR: str = "Acture"
HOME_PAGE: str = "https://github.com/Acture/wotr-turn-based-party-ai"
INFO: str = f"src/{MOD}/Info.json"
PROJECT: str = f"src/{MOD}/{MOD}.csproj"
NEXUS_DESCRIPTION: str = "docs/nexus-description.bbcode"
REQUIRED: tuple[str, ...] = (
	"README.md",
	"CHANGELOG.md",
	"CLAUDE.md",
	"AGENTS.md",
	".github/copilot-instructions.md",
	"LICENSE",
	"LICENSE-EXCEPTION.md",
	NEXUS_DESCRIPTION,
	INFO,
	PROJECT,
	".github/workflows/ci.yml",
)
POINTERS: dict[str, str] = {
	"AGENTS.md": "CLAUDE.md",
	".github/copilot-instructions.md": "../CLAUDE.md",
}
# Game assemblies, Unity data, saves and packages: the game's files and anything built from them.
BINARY_SUFFIXES: frozenset[str] = frozenset(
	{
		".dll",
		".pdb",
		".mdb",
		".exe",
		".so",
		".dylib",
		".assets",
		".bundle",
		".ress",
		".resource",
		".zks",
		".zip",
		".7z",
		".rar",
		".nupkg",
	}
)
LINK: re.Pattern[str] = re.compile(r"\[[^\]\n]*\]\((<[^>\n]+>|[^\s)]+)(?:\s+\"[^\"]*\")?\)")


@dataclass(frozen=True)
class Entry:
	mode: str
	path: str


def boundary_errors(entries: list[Entry]) -> list[str]:
	errors: list[str] = []
	for entry in entries:
		parts: tuple[str, ...] = Path(entry.path).parts
		folded: tuple[str, ...] = tuple(part.casefold() for part in parts)
		if folded[-1] == "status.md" or any(part in {"artifacts", "internal"} for part in folded):
			errors.append(f"Internal documents or run reports are tracked: {entry.path}")
		if any(part in {"bin", "obj", ".vs"} for part in folded):
			errors.append(f"Build output is tracked: {entry.path}")
		if "wrath_data" in folded or Path(folded[-1]).suffix in BINARY_SUFFIXES:
			errors.append(f"Game file, binary or package is tracked: {entry.path}")
		if folded[-1] == "gamepath.props" or folded[-1].endswith(".user"):
			errors.append(f"Local settings are tracked: {entry.path}")
		if any(part in {".obsidian", ".smart-env", ".aws"} for part in parts):
			errors.append(f"Local configuration is tracked: {entry.path}")
		if "credentials-backup" in entry.path.casefold():
			errors.append(f"Credential backup is tracked: {entry.path}")
		if any(
			part.startswith(".env") and part not in {".env.example", ".env.template"}
			for part in parts
		):
			errors.append(f"Environment settings are tracked: {entry.path}")
		if entry.mode == "160000":
			errors.append(f"Submodules are not used in this repository: {entry.path}")
	return errors


def project_property(project: str, name: str) -> str | None:
	match: re.Match[str] | None = re.search(rf"<{name}>([^<]*)</{name}>", project)
	return match[1].strip() if match else None


def metadata_errors(
	info: dict[str, object],
	project: str,
	changelog: str,
	readme: str,
	nexus: str,
	sources: str,
	tag: str | None,
) -> list[str]:
	"""Check Info.json against the project, its entry point and the public texts."""
	errors: list[str] = []
	version: object = info.get("Version")
	if not isinstance(version, str) or not re.fullmatch(r"\d+\.\d+\.\d+", version):
		return [f"Info.json needs a Version like 1.2.3, not {version!r}"]
	expected: dict[str, object] = {
		"Id": MOD,
		"AssemblyName": f"{MOD}.dll",
		"Author": AUTHOR,
		"HomePage": HOME_PAGE,
	}
	for key, value in expected.items():
		if info.get(key) != value:
			errors.append(f"Info.json {key} must be {value!r}, not {info.get(key)!r}")
	if project_property(project, "Version") != version:
		errors.append(
			f"The project's Version {project_property(project, 'Version')!r} "
			f"differs from Info.json's {version!r}"
		)
	if project_property(project, "AssemblyName") != MOD:
		errors.append(f"The project's AssemblyName must be {MOD!r}")
	entry: object = info.get("EntryMethod")
	parts: list[str] = entry.split(".") if isinstance(entry, str) else []
	if len(parts) < 3 or not (
		re.search(rf"\bnamespace\s+{re.escape('.'.join(parts[:-2]))}\b", sources)
		and re.search(rf"\bclass\s+{re.escape(parts[-2])}\b", sources)
		and re.search(rf"\bpublic\s+static\s+bool\s+{re.escape(parts[-1])}\s*\(", sources)
	):
		errors.append(f"Info.json EntryMethod {entry!r} is not a public static bool method")
	if not re.search(r"^## Unreleased\s*$", changelog, re.MULTILINE):
		errors.append("CHANGELOG.md needs an '## Unreleased' section")
	release_heading: str = rf"^## {re.escape(version)} - \d{{4}}-\d{{2}}-\d{{2}}\s*$"
	if not re.search(release_heading, changelog, re.MULTILINE):
		errors.append(f"CHANGELOG.md needs a dated '## {version} - YYYY-MM-DD' section")
	for key in ("GameVersion", "ManagerVersion"):
		value = info.get(key)
		for name, text in (("README.md", readme), (NEXUS_DESCRIPTION, nexus)):
			if not isinstance(value, str) or value not in text:
				errors.append(f"{name} does not name Info.json's {key} {value!r}")
	for needle in (f"Version {version}", f"{MOD}-{version}.zip"):
		if needle not in nexus:
			errors.append(f"{NEXUS_DESCRIPTION} does not name '{needle}'")
	if tag is not None and tag != f"v{version}":
		errors.append(f"Tag {tag!r} does not match Info.json's version {version!r}")
	return errors


def link_errors(root: Path, entries: list[Entry]) -> list[str]:
	"""Relative links in tracked Markdown files must point at files in the repository."""
	errors: list[str] = []
	for entry in entries:
		if not entry.path.endswith(".md"):
			continue
		file: Path = root / entry.path
		fence: str | None = None
		for line in file.read_text(encoding="utf-8").splitlines():
			marker: str = line.lstrip()[:3]
			if marker in {"```", "~~~"}:
				fence = marker if fence is None else None if fence == marker else fence
				continue
			if fence is not None:
				continue
			for match in LINK.finditer(line):
				url = urlsplit(match[1].strip("<>"))
				if url.scheme or url.netloc or not url.path or url.path.startswith("/"):
					continue
				target: Path = (file.parent / unquote(url.path)).resolve()
				if not target.is_relative_to(root) or not target.exists():
					errors.append(f"Broken link: {entry.path} -> {url.path}")
	return errors


def tracked_entries(root: Path) -> list[Entry]:
	result: str = subprocess.check_output(
		["git", "-C", str(root), "ls-files", "--stage", "-z"], text=True, encoding="utf-8"
	)
	entries: list[Entry] = []
	for record in result.split("\0"):
		if not record:
			continue
		metadata, path = record.split("\t", 1)
		mode, _oid, stage = metadata.split()
		if stage != "0":
			raise ValueError(f"Resolve the index conflict before checking: {path}")
		entries.append(Entry(mode, path))
	return entries


def check_repository(root: Path, tag: str | None = None) -> list[str]:
	root = root.resolve()
	entries: list[Entry] = tracked_entries(root)
	tracked: set[str] = {entry.path for entry in entries}
	errors: list[str] = boundary_errors(entries)
	missing: list[str] = [name for name in REQUIRED if name not in tracked]
	errors += [f"Required file is missing: {name}" for name in missing]
	for name, target in POINTERS.items():
		file: Path = root / name
		if name in tracked and file.read_text(encoding="utf-8").strip() != target:
			errors.append(f"{name} must contain only {target}")
	if not missing:

		def read(name: str) -> str:
			return (root / name).read_text(encoding="utf-8")

		sources: str = "\n".join(
			read(entry.path)
			for entry in entries
			if entry.path.startswith(f"src/{MOD}/") and entry.path.endswith(".cs")
		)
		errors += metadata_errors(
			json.loads(read(INFO)),
			read(PROJECT),
			read("CHANGELOG.md"),
			read("README.md"),
			read(NEXUS_DESCRIPTION),
			sources,
			tag,
		)
	errors += link_errors(root, entries)
	return errors


def main() -> None:
	root: Path = Path(__file__).resolve().parents[1]
	tag: str | None = (
		os.environ.get("GITHUB_REF_NAME") if os.environ.get("GITHUB_REF_TYPE") == "tag" else None
	)
	errors: list[str] = check_repository(root, tag)
	if errors:
		raise SystemExit("\n".join(errors))
	print("Tracked files, mod metadata, public texts and links passed.")


if __name__ == "__main__":
	main()
