# Message needs review in #{{.ChannelName}}
A potentially-problematic message was posted in #{{.ChannelName}}:
```
{{.MessageText}}
```
...because it matched this pattern:
```
{{.Pattern}}
```
[Jump to message]({{.JumpLink}})

If this is a false positive, please remove the pattern with `~softunblocktext <pattern>` and correct it before re-inserting it.