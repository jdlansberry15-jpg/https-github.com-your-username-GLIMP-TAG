# Install a Quest APK from a Chromebook

Once a GLIMP TAG APK has been built in Unity Build Automation:

1. Download the APK file from the cloud build artifact page.
2. Open [WebADB](https://webadb.online/) in Google Chrome on the Chromebook.
3. Connect the Quest 2, Quest 3, or Quest 3S with a USB-C cable that supports data.
4. In WebADB, choose **Connect device** and select the Quest from Chrome's USB device picker.
5. Put on the headset and approve the **Allow USB debugging** prompt.
6. Open WebADB's **Install APK** tool, choose the downloaded GLIMP TAG APK, and start the installation.
7. On the headset, open Apps and choose the **Unknown Sources** filter to launch GLIMP TAG.

WebADB advertises one-click APK installation through Chrome/WebUSB without installing ADB or desktop drivers. The Quest connection still depends on Developer Mode, a data-capable cable, Chrome's USB permission, and the headset's debugging approval.

If WebADB does not detect the headset, try another USB-C data cable/port and restart the headset. SideQuest also provides a Chromebook-compatible web installer at [sdq.st/download](https://sdq.st/download), but that is primarily for installing SideQuest's in-VR app.

Installation of this GLIMP TAG build has not been tested yet.
