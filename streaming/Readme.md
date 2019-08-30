Glue42 Streaming Example
==========

# Purpose
Demonstrate the Streaming Interop Model of Glue42.

Included:
- Creation of a streaming endpoint
- Waiting for a streaming endpoint to become available
- Creation of stream branches
- Publishing deltas to stream
- Publishing LastImage at the beginning of a stream subscription as a private (out of band) message
- Validating a subscription request based on parameters, passed by the subscriber
- Rejecting or accepting a subscription based on validation result

# Approach
1) Upon start, a publisher checks if there are any subscribers.
2) Once a subscriber detected, their request details are being examined.
3) If request invalid, subscription gets rejected.
4) If request valid, a stream branch gets created.
5) Upon start, a subscriber subscribes to endpoint availability change.
6) Once an endpoint is available in the Glue42 runtime, it validates if that is the stream searched.
7) If stream validated, the subscriber sends a subscription request with two parameters, selected on a random basis:
	- `reject`, boolean; used as a hardcoded validation argument for the subscriber
	- `DepartmentStore`, enum, used as an indicator, upon which the publisher determines what kind of a
	streaming branch to stream
8) Once subscription successful, publisher sends a private (out of band) message with the LastImage, which
the subscriber uses to build its base context
9) Consequently, the publisher starts/continues to stream data on a specified interval (for the demo purposes,
new "book" data get sent once a second, new "apparel" data gets sent once each third second and new
"food" data gets sent once every five seconds)
10) Publisher adds an entry to its list of subscribers and shows status messages.
11) Subscriber displays status messages.
12) Upon the termination of a subscription (i.e. subscruber application closed), the publisher detects that and
removes the subscriber from the list

# Run
1) Load the Glue42.StreamingExample.sln in Visual Studio
2) Right-click the GlueStreamPiblisher project and select **Debug -> Start New Instance**
3) Right-click the GlueStreamSubscriber project and select **Debug -> Start New Instance** several times
to simulate multiple subscribers and initiate various modes.
	
> It is possible to start Subscribers first and start the Publisher later