# Message templates
All files in this folder must be marked with the Build Action "Embedded resource" in order to be used by the application.

Variables are surrounded with double brackets, and have a dot before the variable name. For example, a variable called `Text`
can be embedded by using `{{.Text}}` in the template.

If the first line of a file starts with a `#` and that file is used to fill a Discord embed, then that line will be used as the title
of the embed.
