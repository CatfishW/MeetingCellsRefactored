# Story System Setup Guide for MeetingCells

## Quick Setup Steps

### 1. Create Story Graph Asset
1. In Unity, right-click in Project window
2. Select `Create > Story System > Story Graph`
3. Name it "MeetingIntroStory" or similar
4. Double-click to open the Story Graph Editor

### 2. Add UI Components to Scene

#### Required UI Prefabs (Create these in your Canvas):

**DialogueUI Component:**
1. Create empty GameObject named "DialogueUI" under your Canvas
2. Add `DialogueUI` script
3. Add UI elements:
   - Panel (dialoguePanel) - background image
   - TextMeshProUGUI (speakerNameText) - speaker name
   - TextMeshProUGUI (dialogueText) - dialogue content
   - Image (speakerPortrait) - speaker portrait (optional)
   - Image (continueIndicator) - "press to continue" indicator

**ChoiceUI Component:**
1. Create empty GameObject named "ChoiceUI" under your Canvas
2. Add `ChoiceUI` script
3. Add UI elements:
   - Panel (choicePanel)
   - TextMeshProUGUI (promptText) - question/prompt
   - Transform (choicesContainer) - parent for buttons
   - ChoiceButtonUI prefab (assign to choiceButtonPrefab)

**ChoiceButtonUI Prefab:**
1. Create a Button with Text
2. Add `ChoiceButtonUI` script
3. Assign button and text references
4. Save as prefab

### 3. Add StoryManager
The StoryManager is auto-created on first use, but you can add a prefab:

1. Create empty GameObject "StoryManager" at scene root
2. Add `StoryManager` script
3. Add any preloaded graphs to the list

### 4. Add Story Triggers

**Point Trigger (for NPCs):**
1. Select NPC GameObject
2. Add Component > Story System > Story Trigger
3. Assign your Story Graph asset
4. Configure trigger settings (Play Once, On Start, etc.)

**Zone Trigger (for areas):**
1. Create empty GameObject "StoryZone"
2. Add BoxCollider (check IsTrigger)
3. Add StoryTriggerZone component
4. Configure trigger on Enter/Exit

### 5. Test Setup

1. Enter Play Mode
2. StoryManager auto-creates itself
3. Walk into trigger zone or talk to NPC
4. Story should play!

## Menu Shortcuts

Available in Unity menus:

- `GameObject > Story System > Story Trigger` - Create point trigger
- `GameObject > Story System > Story Trigger Zone (3D)` - Create 3D zone
- `GameObject > Story System > Story Trigger Zone (2D)` - Create 2D zone
- `Window > Story System > Story Graph Editor` - Open graph editor

## Creating Your First Story

1. Open Story Graph Editor
2. Right-click > Create Node > Start
3. Create Dialogue node
4. Connect Start output to Dialogue input
5. Configure dialogue text and speaker
6. Create End node
7. Connect Dialogue to End
8. Save graph
9. Assign to trigger

## Common Issues

**No UI showing:**
- Check Canvas is in Screen Space - Overlay mode
- Verify UI scales for your resolution
- Check DialogueUI/ChoiceUI references are assigned

**Story not triggering:**
- Verify trigger has correct tag (Player)
- Check Story Graph is assigned
- Ensure Start node exists in graph

**Dialogue doesn't advance:**
- Click on dialogue panel or press input
- Check StoryManager.Instance exists
- Verify DialogueUI is subscribed to events
