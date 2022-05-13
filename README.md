# High Ping Kicker

[![Tested with A20.4 b42](https://img.shields.io/badge/A20.4%20b42-tested-blue.svg)](https://7daystodie.com/) [![Automated Release](https://github.com/fatal-expedition/nerf-parkour/actions/workflows/main.yml/badge.svg)](https://github.com/jonathan-robertson/gmo-farming/actions/workflows/main.yml)

7 Days to Die Modlet: Automatically monitor and kick/ban players with excessively high ping

## Special Thanks Up Front

This mod is largely based on the feature by the same name in [Server Tools](https://github.com/dmustanger/7dtd-ServerTools). I would highly encourage anyone interested in *this* mod to check out their server management mod as well. It comes with a full suite of AntiCheat services and a truly wild number of tools and capabilities.

## Why not just use Server Tools?

1. Accessibility: I found that my current host and many other hosts (it turns out) do not support the creation/management of xml files in the game root directory and instead rely on configuration via admin commands. In order to ensure accessibility and ease of use for all admins in any situation, **High Ping Kicker (this project) is fully configurable from the admin console or via Telnet**.
2. Compartmentalization: I'm a fan of keeping mods as small and as simple as possible, which aligns more to a 'microservices' line of thinking. There are some downsides to this, in certain situations, but monitoring/kicking players for excessive ping would not suffer from this approach in my view.

## Usage

*Ping limit will come preconfigured with a suggested value of 200ms, but this might naturally exclude some regions that you don't want to exclude... consider registering your server at <https://7daystodie-servers.com/> and view the Ping tab to get a rundown of ping tests from various parts of the world.*

1. TODO
