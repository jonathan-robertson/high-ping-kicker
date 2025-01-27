﻿# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2022-07-21

- fix initial load in brand new map

## [1.0.1] - 2022-05-28

- fix bug allowing LagFailures to not get cleaned up

## [1.0.0] - 2022-05-16

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
