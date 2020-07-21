#!/bin/bash
source_dir=$(xcrun simctl get_app_container booted no.blueye.MapsuiTest data)
open $source_dir