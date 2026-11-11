# Galaxy Buds3 transport

This is an independent, minimal implementation of Samsung's reverse-engineered SPP wire format, limited to Galaxy Buds3 (SM-R530). It does not include firmware, reset, diagnostics, tracking or account commands. No upstream application code is vendored.

Protocol research references (consulted 2026-11-11):

- https://github.com/timschneeb/GalaxyBudsClient/blob/master/GalaxyBudsClient/Model/Specifications/Buds3DeviceSpec.cs
- https://github.com/timschneeb/GalaxyBudsClient/blob/master/GalaxyBudsClient/Model/Constants.cs
- https://github.com/timschneeb/GalaxyBudsClient/blob/master/GalaxyBudsClient/Message/SppMessage.cs
- https://github.com/timschneeb/GalaxyBudsClient/blob/master/GalaxyBudsClient/Message/Decoder/StatusUpdateDecoder.cs
- https://github.com/timschneeb/GalaxyBudsClient/blob/master/GalaxyBudsClient/Message/Decoder/ExtendedStatusUpdateDecoder.cs
- https://github.com/timschneeb/GalaxyBudsClient/blob/master/GalaxyBudsClient/Message/Encoder/ManagerInfoEncoder.cs
- https://github.com/timschneeb/GalaxyBudsClient/blob/master/GalaxyBudsClient/Utils/Crc16.cs
- https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/send-or-receive-files-with-rfcomm

Service UUID: `2e73a4ad-332d-41fc-90e2-16bef06523f2`.
Frame: `FD size-low size-high|flags id payload crc-low crc-high DD`.
Size includes ID and CRC, not delimiters/header. The size field has ten bits. CRC-16/XMODEM: polynomial 0x1021, initial 0, calculated over ID + payload. Response bit: 0x1000; fragment bit: 0x2000 (ignored by this client).

Read messages:

| ID | Fields (zero-based payload offsets) |
|---|---|
| 0x60 | Battery L=1, R=2, case=6 |
| 0x61 | Battery L=2, R=3, case=7; noise mode=12 |
| 0x77 | Noise mode=0 |
| 0x51 | Command ID=0, result=1 (zero means success) |

Write messages: initial status response, 0x88 manager info `[1,2,34]`, 0x78 noise mode `[0]` (off) or `[1]` (ANC). The UI does not assume success until the device reports matching state or a successful command response. A five-second timeout leaves the prior state unchanged.

Values above 100 are unavailable, not percentages. Unknown case data is not retained as fresh. Missing or unsupported ANC mode disables controls. An unavailable device never receives commands. Data is pushed by the earbuds; timestamps show when battery data last arrived, not a fabricated polling time.

Hardware validation still required: physical Buds3 firmware revisions, case-open/closed transitions, charging, one-bud operation, control-channel arbitration with Galaxy Wearable, suspend/resume and Bluetooth-adapter changes.
