This demo demonstrate a simple usage of Glue42 Context API for .Net. It contains two apps:
* SharedContextPub - updates a shared context called *TestContext* every second, incrementing a field called data in the context
* SharedContextSub - listens for updates in *TestContext* and prints them on the console