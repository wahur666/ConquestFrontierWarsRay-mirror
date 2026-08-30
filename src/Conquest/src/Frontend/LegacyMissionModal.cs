using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacyMissionModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private const int MissionButtonBegin = 231200;
	private const int MovieButtonBegin = 331200;
	private const int MissionTitleBegin = 1824;
	private const int MissionDescriptionBegin = 1800;
	private const int MovieTitleBegin = 1841;
	private const int MovieDescriptionBegin = 1817;
	private static readonly Color MissionLineColor = new(10, 182, 255, 255);
	private static readonly Color MovieLineColor = new(255, 255, 44, 255);

	private readonly GT_MENU1_SELECT_MISSION _menuData;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly LegacyRcStringResolver _strings;
	private readonly Action _closed;
	private readonly List<AtlasFramesResource> _atlasResources = [];
	private readonly List<LegacyButtonNode> _missionButtons = [];
	private readonly List<LegacyButtonNode> _movieButtons = [];
	private readonly List<LineSegment> _missionLines = [];
	private readonly List<LineSegment> _movieLines = [];
	private LegacyListBoxNode? _missionList;
	private LegacyStaticNode? _missionDescription;
	private LegacyStaticNode? _missionTitle;
	private LegacyStaticNode? _missionHolder;
	private LegacyStaticNode? _movieHolder;
	private LegacyButtonNode? _backButton;
	private LegacyButtonNode? _okButton;
	private LegacyButtonNode? _cancelButton;
	private LegacyButtonNode? _debugStartButton;
	private LegacyButtonNode? _debugUnlockButton;
	private LegacyBriefingModal? _briefingModal;
	private LegacyButtonNode? _briefingReturnFocusButton;
	private int _currentMissionSelection = -1;
	private int _currentMovieSelection = -1;

	public LegacyMissionModal(
		GT_MENU1_SELECT_MISSION menuData,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action closed) : base("LegacyMissionModal", closed) {
		_menuData = menuData;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_closed = closed;
	}

	protected override void OnInitialize() {
		base.OnInitialize();
		ContentRoot.Position = ResolveScreenPosition(_menuData.ScreenRect);

		AddStaticNode("Background", _menuData.Background);
		AddStaticNode("Title", _menuData.Title);
		_missionList = AddListBoxNode("MissionList", _menuData.List);
		_debugStartButton = AddButtonNode("DebugStart", _menuData.Start);
		_okButton = AddButtonNode("Ok", _menuData.Ok);
		_cancelButton = AddButtonNode("Cancel", _menuData.Cancel);
		_debugUnlockButton = AddButtonNode("Unlock", _menuData.ButtonUnlock);
		_backButton = AddButtonNode("Back", _menuData.ButtonBack);

		_movieHolder = AddStaticNode("MovieHolder", _menuData.StaticMovie);
		_missionHolder = AddStaticNode("MissionHolder", _menuData.StaticHolder);
		_missionDescription = AddStaticNode("MissionDescription", _menuData.StaticMission);
		_missionTitle = AddStaticNode("MissionTitle", _menuData.StaticMissionTitle);

		AddAnimationNode("SystemAnimation", _menuData.AnimSystem);
		BuildMissionButtons();
		BuildMovieButtons();
		PopulateMissionList();

		if (_missionList is not null) {
			_missionList.SelectionCommitted += _ => CommitListSelection();
			_missionList.CommitOnDoubleClick = true;
			_missionList.SetVisible(false);
		}

		if (_okButton is not null) {
			_okButton.Activated += _ => CommitListSelection();
			_okButton.SetVisible(false);
		}

		if (_cancelButton is not null) {
			_cancelButton.Activated += _ => CloseModal();
			_cancelButton.SetVisible(false);
		}

		if (_backButton is not null) {
			_backButton.Activated += _ => CloseModal();
		}

		if (_debugStartButton is not null) {
			_debugStartButton.Activated += _ => ShowStatus("Mission runtime start is not ported yet.");
			_debugStartButton.SetVisible(false);
			_debugStartButton.EnableButton(false);
		}

		if (_debugUnlockButton is not null) {
			_debugUnlockButton.Activated += _ => UnlockAll();
			_debugUnlockButton.SetVisible(false);
			_debugUnlockButton.EnableButton(false);
		}

		SetInfoVisible(false, showMovieHolder: false);
		if (_missionButtons.Count > 0) {
			_currentMissionSelection = 0;
			ApplyMissionSelection(_currentMissionSelection, setFocus: true);
		}
	}

	protected override void OnDispose() {
		for (var index = _atlasResources.Count - 1; index >= 0; index--) {
			_atlasResources[index].Dispose();
		}

		_atlasResources.Clear();
		base.OnDispose();
	}

	protected override void Draw() {
		base.Draw();

		foreach (var line in _missionLines) {
			if (!line.Visible()) {
				continue;
			}

			DrawLine(line.Start, line.End, MissionLineColor);
		}

		foreach (var line in _movieLines) {
			if (!line.Visible()) {
				continue;
			}

			DrawLine(line.Start, line.End, MovieLineColor);
		}
	}

	private void BuildMissionButtons() {
		for (var index = 0; index < _menuData.ButtonMissions.Length; index++) {
			var button = AddButtonNode($"Mission{index + 1}", _menuData.ButtonMissions[index]);
			if (button is null) {
				continue;
			}

			button.ControlId = (uint)(MissionButtonBegin + index);
			button.Activated += _ => OpenMission(index);
			button.Entered += _ => ApplyMissionSelection(index, setFocus: true);
			_missionButtons.Add(button);
		}

		var buttonCenters = new Vector2[_missionButtons.Count];
		for (var index = 0; index < _missionButtons.Count; index++) {
			var button = _missionButtons[index];
			buttonCenters[index] = button.Position + (button.Size * 0.5f);
		}

		var lineInset = 20f;
		for (var index = 1; index < _missionButtons.Count; index++) {
			var fromIndex = index < _menuData.NLineFrom.Length ? _menuData.NLineFrom[index] : -1;
			if (fromIndex < 0 || fromIndex >= buttonCenters.Length) {
				continue;
			}

			var start = buttonCenters[index];
			var end = buttonCenters[fromIndex];
			if (!TryInsetLine(start, end, lineInset, out var insetStart, out var insetEnd)) {
				continue;
			}

			_missionLines.Add(new LineSegment(
				insetStart,
				insetEnd,
				() => index < _missionButtons.Count && _missionButtons[index].Visible));
		}
	}

	private void BuildMovieButtons() {
		for (var index = 0; index < _menuData.ButtonMovies.Length; index++) {
			var button = AddButtonNode($"Movie{index + 1}", _menuData.ButtonMovies[index]);
			if (button is null) {
				continue;
			}

			button.ControlId = (uint)(MovieButtonBegin + index);
			button.Activated += _ => ApplyMovieSelection(index, setFocus: true);
			button.Entered += _ => ApplyMovieSelection(index, setFocus: true);
			_movieButtons.Add(button);
		}

		var missionCenters = new Vector2[_missionButtons.Count];
		for (var index = 0; index < _missionButtons.Count; index++) {
			missionCenters[index] = _missionButtons[index].Position + (_missionButtons[index].Size * 0.5f);
		}

		var movieCenters = new Vector2[_movieButtons.Count];
		for (var index = 0; index < _movieButtons.Count; index++) {
			movieCenters[index] = _movieButtons[index].Position + (_movieButtons[index].Size * 0.5f);
		}

		var lineInset = 24f;
		for (var index = 0; index < _menuData.ButtonMovies.Length; index++) {
			if (index >= _movieButtons.Count) {
				break;
			}

			var beforeMission = index < _menuData.NMovieBeforeMission.Length ? _menuData.NMovieBeforeMission[index] : 0;
			if (beforeMission > 0) {
				if (beforeMission >= missionCenters.Length || beforeMission >= _menuData.NLineFrom.Length) {
					continue;
				}

				var firstMissionIndex = beforeMission;
				var secondMissionIndex = _menuData.NLineFrom[firstMissionIndex];
				if (secondMissionIndex < 0 || secondMissionIndex >= missionCenters.Length) {
					continue;
				}

				var midpoint = (missionCenters[firstMissionIndex] + missionCenters[secondMissionIndex]) * 0.5f;
				if (!TryInsetLine(movieCenters[index], midpoint, lineInset, out var start, out _)) {
					continue;
				}

				_movieLines.Add(new LineSegment(
					midpoint,
					start,
					() => index < _movieButtons.Count && _movieButtons[index].Visible));
				continue;
			}

			var missionIndex = -beforeMission;
			if (missionIndex < 0 || missionIndex >= missionCenters.Length) {
				continue;
			}

			if (!TryInsetLine(movieCenters[index], missionCenters[missionIndex], lineInset, out var movieEdge, out var missionEdge)) {
				continue;
			}

			_movieLines.Add(new LineSegment(
				movieEdge,
				missionEdge,
				() => index < _movieButtons.Count && _movieButtons[index].Visible));
		}
	}

	private void PopulateMissionList() {
		if (_missionList is null) {
			return;
		}

		_missionList.ResetContent();
		foreach (var fileName in EnumerateCampaignMissionFiles()) {
			_missionList.AddString(fileName);
		}

		if (_missionList.GetNumberOfItems() > 0) {
			_missionList.SetCurrentSelection(0);
		}
	}

	private IEnumerable<string> EnumerateCampaignMissionFiles() {
		var directoryPath = Path.Combine(RepoPaths.LocateRepoRoot(), "Maps", "Campaign");
		if (!Directory.Exists(directoryPath)) {
			yield break;
		}

		foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.dmission", SearchOption.TopDirectoryOnly)) {
			var fileName = Path.GetFileName(filePath);
			if (!string.IsNullOrWhiteSpace(fileName)) {
				yield return fileName;
			}
		}

		foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.qmission", SearchOption.TopDirectoryOnly)) {
			var fileName = Path.GetFileName(filePath);
			if (!string.IsNullOrWhiteSpace(fileName)) {
				yield return fileName;
			}
		}
	}

	private void ApplyMissionSelection(int index, bool setFocus) {
		if ((uint)index >= _missionButtons.Count) {
			return;
		}

		_currentMissionSelection = index;
		_currentMovieSelection = -1;
		SetInfoVisible(true, showMovieHolder: false);

		if (_missionTitle is not null) {
			_missionTitle.SetText(ResolveString((uint)(MissionTitleBegin + index + 1), $"Mission {index + 1}"));
		}

		if (_missionDescription is not null) {
			_missionDescription.SetText(ResolveString((uint)(MissionDescriptionBegin + index + 1), $"Mission {index + 1} briefing data is not authored yet."));
		}

		if (setFocus) {
			_missionButtons[index].SetKeyboardFocus(true);
		}
	}

	private void ApplyMovieSelection(int index, bool setFocus) {
		if ((uint)index >= _movieButtons.Count) {
			return;
		}

		_currentMovieSelection = index;
		_currentMissionSelection = -1;
		SetInfoVisible(true, showMovieHolder: true);

		if (_missionTitle is not null) {
			_missionTitle.SetText(ResolveString((uint)(MovieTitleBegin + index + 1), $"Movie {index + 1}"));
		}

		if (_missionDescription is not null) {
			_missionDescription.SetText(ResolveString((uint)(MovieDescriptionBegin + index + 1), "Movie playback is not ported yet."));
		}

		if (setFocus) {
			_movieButtons[index].SetKeyboardFocus(true);
		}
	}

	private void SetInfoVisible(bool visible, bool showMovieHolder) {
		_missionHolder?.SetVisible(visible && !showMovieHolder);
		_movieHolder?.SetVisible(visible && showMovieHolder);
	}

	private void CommitListSelection() {
		if (_missionList is null) {
			return;
		}

		var selectedIndex = _missionList.GetCurrentSelection();
		if (selectedIndex < 0) {
			return;
		}

		var fileName = _missionList.GetString(selectedIndex);
		if (string.IsNullOrWhiteSpace(fileName)) {
			return;
		}

		OpenBriefing(fileName, _okButton ?? _backButton);
	}

	private void OpenMission(int missionIndex) {
		if ((uint)missionIndex >= _missionButtons.Count) {
			return;
		}

		var dmission = $"T_Mission{missionIndex + 1}.dmission";
		var qmission = $"T_Mission{missionIndex + 1}.qmission";
		var missionFile = HasMissionFile(dmission) ? dmission : qmission;
		OpenBriefing(missionFile, _missionButtons[missionIndex]);
	}

	private void OpenBriefing(string missionFileName, LegacyButtonNode? returnFocusButton) {
		if (_briefingModal is not null) {
			return;
		}

		_briefingReturnFocusButton = returnFocusButton;
		SetInteractiveState(false);
		_briefingModal = AddChild(new LegacyBriefingModal(
			missionFileName,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			CloseBriefing));
	}

	private void CloseBriefing() {
		if (_briefingModal is not null && RemoveChild(_briefingModal)) {
			_briefingModal.Dispose();
		}

		_briefingModal = null;
		SetInteractiveState(true);
		(_briefingReturnFocusButton ?? _backButton)?.SetKeyboardFocus(true);
		_briefingReturnFocusButton = null;
	}

	private void UnlockAll() {
		foreach (var button in _missionButtons) {
			button.SetVisible(true);
		}

		foreach (var button in _movieButtons) {
			button.SetVisible(true);
		}
	}

	private bool HasMissionFile(string fileName) {
		if (_missionList is not null && _missionList.FindStringExact(fileName) >= 0) {
			return true;
		}

		var path = Path.Combine(RepoPaths.LocateRepoRoot(), "Maps", "Campaign", fileName);
		return File.Exists(path);
	}

	private void SetInteractiveState(bool enabled) {
		foreach (var button in _missionButtons) {
			button.EnableButton(enabled && button.Visible);
		}

		foreach (var button in _movieButtons) {
			button.EnableButton(enabled && button.Visible);
		}

		_backButton?.EnableButton(enabled);
		_okButton?.EnableButton(enabled);
		_cancelButton?.EnableButton(enabled);
		_missionList?.EnableListbox(enabled);
	}

	private void ShowStatus(string message) {
		if (_missionDescription is not null) {
			_missionDescription.SetText(message);
		}
	}

	private void CloseModal() {
		if (_briefingModal is not null) {
			CloseBriefing();
		}

		_closed();
	}

	private LegacyButtonNode? AddButtonNode(string label, BUTTON_DATA data) {
		if (string.IsNullOrWhiteSpace(data.ButtonType)) {
			return null;
		}

		var archetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", data.ButtonType);
		var node = new LegacyButtonNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.ButtonText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.Text = text;
		}

		ContentRoot.AddChild(node);
		return node;
	}

	private LegacyListBoxNode AddListBoxNode(string label, LISTBOX_DATA data) {
		var archetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", data.ListboxType);
		var node = new LegacyListBoxNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository, _xmlDbRepository);
		ContentRoot.AddChild(node);
		return node;
	}

	private LegacyStaticNode AddStaticNode(string label, STATIC_DATA data) {
		var archetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", data.StaticType);
		var node = new LegacyStaticNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.StaticText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.SetText(text);
		}

		ContentRoot.AddChild(node);
		return node;
	}

	private AnimatedSprite2D? AddAnimationNode(string label, ANIMATE_DATA data) {
		var archetype = ReadTypedEntry<GT_ANIMATE>("GT_ANIMATE", data.AnimateType);
		var vfxData = _vfxRepository.Load();
		if (!vfxData.TryGetAtlasByVfxShapeId(archetype.VfxType, out var atlasEntry)) {
			return null;
		}

		var atlas = new AtlasFramesResource(
			_vfxRepository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: false),
			_vfxRepository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: true));
		_atlasResources.Add(atlas);

		var node = new AnimatedSprite2D(atlas.Frames, label) {
			Position = new Vector2(data.XOrigin, data.YOrigin),
			Pivot = Vector2.Zero,
			Loop = true,
			SpeedFps = data.Timer > 0 ? 1000f / data.Timer : 15f
		};
		ContentRoot.AddChild(node);
		return node;
	}

	private string ResolveString(uint id, string fallback) {
		return _strings.TryResolve(id, out var text) && !string.IsNullOrWhiteSpace(text)
			? text
			: fallback;
	}

	private T ReadTypedEntry<T>(string typeName, string fileName) where T : class {
		var details = _xmlDbRepository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
		       ?? throw new InvalidOperationException(
			       $"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}

	private static bool TryInsetLine(Vector2 start, Vector2 end, float inset, out Vector2 insetStart, out Vector2 insetEnd) {
		var direction = end - start;
		var length = direction.Length();
		if (length <= float.Epsilon) {
			insetStart = start;
			insetEnd = end;
			return false;
		}

		var unit = direction / length;
		insetStart = start + (unit * inset);
		insetEnd = end - (unit * inset);
		return true;
	}

	private static Vector2 ResolveScreenPosition(RECT screenRect) {
		var width = Math.Max(0f, screenRect.Right - screenRect.Left);
		var height = Math.Max(0f, screenRect.Bottom - screenRect.Top);
		return new Vector2(
			(LegacyScreenWidth - width) * 0.5f,
			(LegacyScreenHeight - height) * 0.5f);
	}

	private sealed record LineSegment(Vector2 Start, Vector2 End, Func<bool> Visible);
}
