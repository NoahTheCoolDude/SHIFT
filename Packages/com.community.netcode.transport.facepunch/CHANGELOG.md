# Changelog
All notable changes to this package will be documented in this file. The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)

## Unreleased — SHIFT local fork

Vendored into this repository as an embedded package, forked from upstream
[multiplayer-community-contributions](https://github.com/Unity-Technologies/multiplayer-community-contributions)
at commit `27d3e825ecdd`.

Embedded rather than consumed via git URL because upstream is effectively unmaintained —
its last release targets NGO 1.0.0 (2022) and `main` does not compile. Edits under
`Library/PackageCache/` are wiped on every package resolve, so the source must live in
`Packages/` to stay modifiable and to reach both developers through the repo.

### Fixed
- Removed a stray unmatched `#endregion` at the end of `Runtime/FacepunchTransport.cs`
  (three `#region` directives against four `#endregion`), which failed compilation with
  `error CS1028: Unexpected preprocessor directive`.

### Known issues — present upstream, not yet addressed here
- `GetCurrentRtt()` returns a hardcoded `0`, so NGO's built-in RTT reporting reads zero
  through this transport. Real values require Steam's `Connection.QuickStatus()`.
- The transport owns the Steam lifecycle: `SteamClient.Init()` in `Initialize()` and
  `SteamClient.Shutdown()` in `Shutdown()`. A lobby layer must consume the
  already-initialized client rather than initializing its own.
- The declared dependency `com.unity.netcode.gameobjects: 1.0.0-pre.4` is stale metadata.
  UPM treats it as a minimum, not a pin; verified compiling against NGO 2.13.1 on
  Unity 6000.5.6f1.

## 2.0.0

### Changed
- Targets the Netcode for GameObjects 1.0.0 package.
- Renamed namespaces from MLAPI to Netcode.

### Removed
- Removed support for channels.
- No longer send 1 byte of channel information in each message.

## 1.0.0
First version of the Facepunch Transport as a Unity package.