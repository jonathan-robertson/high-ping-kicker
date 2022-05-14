﻿# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [TODO]

- add option for system to whisper to users about latency
- add exception for admins
- add option for ping requirement on login separate from while playing to early-bump

## [1.0.0] - 2022-05-13

- add command to show current ping and kick counters
- add logs entries any time a counter increases
- flush ping counter when players is kicked
- flush ping & kick counter when player is banned
- update existing log entries with standard prefix
- include admin console commands
- support live settings updates
- include default values of the following:
  - `MaxPingAllowed`: 200 (ms)
  - `FailureThresholdBeforeKick`: 2 (instances)
  - `AllowedKicksBeforeBan`: 2 (instances)
  - `HoursBannedAfterKickWarnings`: 24 (hours)