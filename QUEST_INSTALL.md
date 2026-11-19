# Install a Quest APK from a Chromebook

This is the planned no-PC installation path after Unity Build Automation produces a GLIMP TAG APK.

## Before connecting

- Meta Quest 2, Quest 3, or Quest 3S.
- Meta Developer Mode enabled for the headset.
- A USB-C cable that supports data (not charge-only).
- Google Chrome on the Chromebook.
- The downloaded GLIMP TAG `.apk` file.

Developer Mode and USB debugging approval are Meta headset/account requirements. They cannot be bypassed by a browser sideloader.

## Browser installation with QuestLoad

1. Download the GLIMP TAG APK from the Unity Build Automation artifact page.
2. Open [QuestLoad](https://frizzlem.github.io/QuestLoad/) in Chrome on the Chromebook.
3. Connect the Quest to the Chromebook with the USB-C data cable.
4. Choose **Connect Quest** and select the headset in Chrome's USB device picker.
5. Put on the headset and approve **Allow USB debugging**.
6. In QuestLoad, choose the downloaded `.apk` file and select **Install APK**.
7. Wait for the install log to report success.
8. On the headset, open Apps → **Unknown Sources** and launch GLIMP TAG.

QuestLoad documents support for Quest 2, Quest 3, Quest 3S, and Chrome/Edge browsers with WebUSB. See the [QuestLoad project and requirements](https://github.com/FrizzleM/QuestLoad).

## Alternative browser sideloader

[Quest Webloader](https://webloader.app/) also uses WebUSB in Chrome and supports APK installation without a local ADB binary. It requires the same Developer Mode, data cable, and USB debugging approval.

## Troubleshooting

- If Chrome does not show the headset, try another USB-C data cable or Chromebook port.
- Unlock the headset and approve the USB debugging prompt inside the headset.
- Confirm Developer Mode is enabled before reconnecting.
- Keep the sideloader tab open until installation finishes.

## Status

This is an installation guide only. GLIMP TAG does not have a verified APK yet, and the installation procedure has not been tested with this project.
