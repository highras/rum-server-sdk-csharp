# rum-server-sdk-csharp

## Usage

### Create

```
RUMServerClient client = new RUMServerClient(41000015, "xxxxxx-xxxxx-xxxx-xxxx-xxxxxxx", "52.83.220.166", 13609, true, 5000);
```

### Set Rum ID And Session ID . (Optional, If not specified, a random one will be generated)
```
client.SetRumId(string rid);
client.SetSessionId(long sid)
```

### Send Custom Event
```
Hashtable attrs = new Hashtable();
attrs["test"] = 123;
attrs["xxx"] = "yyy";

client.SendCustomEvent("error", attrs, 5000, (exception) =>
{
     if (exception != null)
         Console.WriteLine(exception.Message);
     else
         Console.WriteLine("send ok");
});
```

### Send Custom Events
```
ArrayList events = new ArrayList();

Hashtable ev1 = new Hashtable();
ev1["ev"] = "error";
ev1["attrs"] = attrs;

Hashtable ev2 = new Hashtable();
ev2["ev"] = "info";
ev2["attrs"] = attrs;

events.Add(ev1);
events.Add(ev2);

client.SendCustomEvents(events, 5000, (exception) =>
{
    if (exception != null)
        Console.WriteLine(exception.Message);
    else
        Console.WriteLine("send ok");
});
```
