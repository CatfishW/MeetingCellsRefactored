# Fall Animation Setup Guide

## Current Status
The Fall animation state is currently using the Jump animation as a placeholder.
To replace it with a proper falling animation, follow the steps below.

## Option 1: Mixamo (Recommended - Free)

### Step 1: Download from Mixamo
1. Go to https://www.mixamo.com/
2. Sign in with your Adobe account (free)
3. In the animation search bar, type one of these:
   - "fall"
   - "falling"
   - "free fall loop"
   - "drop"
   - "dive"
4. Select an animation (recommended: "Falling" or "Free Fall Loop")
5. Click "Download"
6. Settings:
   - **Format**: FBX for Unity (.fbx)
   - **Skin**: Without Skin
   - **Frames per Second**: 30
   - **Keyframe Reduction**: None
   - Check **"In Place"** if you want the character to fall in place (recommended for game physics)
7. Click "Download" and save to `Assets/MeetingCellsRefactored/Animations/`

### Step 2: Import to Unity
1. The FBX will automatically import
2. Select the FBX file in Unity
3. In the Inspector:
   - **Rig** tab:
     - Animation Type: **Humanoid**
     - Avatar Definition: **Create From This Model**
   - Click **Apply**

### Step 3: Assign to Animator Controller
1. Open `Customized_Character_Anime.controller`
2. Find the **"Fall"** state (shown in orange in the Animator window)
3. Click on the Fall state
4. In the Inspector, under **Motion**, drag your new fall animation clip
5. The animation is now assigned!

## Option 2: Unity Asset Store - Huge FBX Mocap Library (Free)

### Step 1: Download from Asset Store
1. Open Unity Asset Store window (Window > Asset Store)
2. Search for: **"Huge FBX Mocap Library Part 1"**
3. Click **Add to My Assets** (it's FREE)
4. In Unity, open Package Manager (Window > Package Manager)
5. Switch to "My Assets" and download/import the package

### Step 2: Find Fall Animation
1. Look through the imported animations in `Assets/FBX Mocap Library/`
2. Common fall animation names:
   - `fall_forward.fbx`
   - `fall_backward.fbx`
   - `falling.fbx`
   - `drop.fbx`

### Step 3: Assign to Controller
Same as Option 1, Step 3 above.

## Animation State Details

The Animator Controller already has these transitions set up:

```
Any State → Fall (when IsFalling = true)
Fall → Idle (when Grounded = true)
JumpV2 → Fall (when IsFalling = true)
```

The `IsFalling` parameter is automatically set by the `CinemachinePlayerController` script when:
- Player is not grounded
- Player's vertical velocity is less than -0.5 (falling down)

## Testing the Animation

1. Enter Play Mode
2. Walk to the edge of a platform or jump
3. When falling, the character should play the fall animation
4. When landing, it should transition back to Idle

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Animation doesn't play | Check that the Avatar in the FBX is set to Humanoid |
| Character rotates weirdly | In the Animation import settings, check "Bake Into Pose" for Root Rotation |
| Animation too fast/slow | Adjust the **Speed** parameter in the Animator state |
| Falls through ground | Ensure the ground has a collider and is in the GroundMask layer |

## Recommended Fall Animations from Mixamo

1. **"Falling"** - Generic falling pose (good for short falls)
2. **"Free Fall Loop"** - Continuous falling (good for long drops/skydiving)
3. **"Falling Forward Death"** - Dramatic fall (if you want a collapse animation)
4. **"Drop"** - Quick drop animation

All of these work well with the existing controller setup!
