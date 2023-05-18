# RCON
[Source RCON Protocol](https://developer.valvesoftware.com/wiki/Source_RCON_Protocol) implementation in C# for Minecraft server

# Usage

Create RconClient instance
``` C#
RconClient client = new RconClient();
```
Connect to the server
```C#
await client.ConnectAsync("id addr", port);
```
Authenticate
```C#
bool result = await client.AuthAsync("password");
```
Send commands
```C#
List<Packet> response = await client.ExecCommandAsync("command", id, timeout);
```
Close connection
```C#
client.Close();
```

# Formatters

When you call `ExecCommandAsync()`,  you can use `RconFormatter`.  
This is a function that is called after the command is executed and formats the received response.

Formatter example:
```C#
static string Formmater(string command, int ID, List<Packet> response)
{
	var stringBuilder = new StringBuilder();
	foreach (var item in result)
	{
		stringBuilder.Append(item.Body);
	}
	return stringBuilder.ToString();
}
```
This formatter will convert any server response to a string.

And it can be applied as follows:
```C#
var response = await client.ExecCommandAsync("command", id, Formmater, timeout)
```
