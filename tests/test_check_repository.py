"""Tests for check_repository.py: the repository passes, and each kind of mistake is caught."""

from __future__ import annotations

import json
import unittest
from pathlib import Path

from check_repository import (
	INFO,
	NEXUS_DESCRIPTION,
	PROJECT,
	Entry,
	boundary_errors,
	check_repository,
	metadata_errors,
)

ROOT: Path = Path(__file__).resolve().parents[1]


def read(name: str) -> str:
	return (ROOT / name).read_text(encoding="utf-8")


class RepositoryTests(unittest.TestCase):
	def test_the_repository_passes(self) -> None:
		self.assertEqual(check_repository(ROOT), [])

	def test_game_files_and_local_files_are_refused(self) -> None:
		paths: list[str] = [
			"lib/Assembly-CSharp.dll",
			"Wrath_Data/Managed/readme.txt",
			"saves/Manual_1.zks",
			"TurnBasedPartyAI-0.0.1.zip",
			"artifacts/report.json",
			"STATUS.md",
			"GamePath.props",
			"src/TurnBasedPartyAI/bin/Debug/x.txt",
			".env",
		]
		errors: list[str] = boundary_errors([Entry("100644", path) for path in paths])
		for path in paths:
			self.assertTrue(any(error.endswith(path) for error in errors), path)
		self.assertEqual(boundary_errors([Entry("100644", "src/TurnBasedPartyAI/Main.cs")]), [])


class MetadataTests(unittest.TestCase):
	def setUp(self) -> None:
		self.info: dict[str, object] = json.loads(read(INFO))
		self.texts: dict[str, str] = {
			"project": read(PROJECT),
			"changelog": read("CHANGELOG.md"),
			"readme": read("README.md"),
			"nexus": read(NEXUS_DESCRIPTION),
			"sources": read("src/TurnBasedPartyAI/Main.cs"),
		}

	def errors(self, tag: str | None = None) -> list[str]:
		return metadata_errors(self.info, tag=tag, **self.texts)

	def test_the_current_metadata_passes(self) -> None:
		version: object = self.info["Version"]
		self.assertEqual(self.errors(), [])
		self.assertEqual(self.errors(tag=f"v{version}"), [])

	def test_a_version_changed_in_one_place_is_caught(self) -> None:
		self.info["Version"] = "9.9.9"
		errors: list[str] = self.errors()
		self.assertTrue(any("project's Version" in error for error in errors), errors)
		self.assertTrue(any("CHANGELOG.md needs a dated" in error for error in errors), errors)
		self.assertTrue(any(NEXUS_DESCRIPTION in error for error in errors), errors)

	def test_a_tag_for_another_version_is_caught(self) -> None:
		self.assertTrue(any("Tag 'v9.9.9'" in error for error in self.errors(tag="v9.9.9")))

	def test_an_entry_method_that_does_not_exist_is_caught(self) -> None:
		self.info["EntryMethod"] = "TurnBasedPartyAI.Main.Start"
		self.assertTrue(any("EntryMethod" in error for error in self.errors()))

	def test_another_author_is_caught(self) -> None:
		self.info["Author"] = "AC"
		self.assertTrue(any("Author" in error for error in self.errors()))


if __name__ == "__main__":
	unittest.main()
