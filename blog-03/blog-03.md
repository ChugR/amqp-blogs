# AMQP Illustrated
This blog post exposes some of the details of the [AMQP protocol version 1.0](http://docs.oasis-open.org/amqp/core/v1.0/os/amqp-core-complete-v1.0-os.pdf). Code from the previous [HelloWorld example](https://people.apache.org/~chug/blog/wordpress-01/helloworld.cs.html) is traced by [Wireshark](https://www.wireshark.org/) to capture the over-the-wire protocol details. The trace is rendered into a web page by [Adverb](https://github.com/ChugR/Adverb), my AMQP comprehension tool.

HelloWorld is an unrealistically simple use of AMQP. Real world applications need any number of more advanced techniques. That said, HelloWorld generates instances of every AMQP performative. For folks new to AMQP seeing lines of C# code together with the over-the-wire protocol packets will speed the learning process. 

The goal here is to orient the reader and to expose all the details of a successful AMQP protocol operation. 

# Test Setup

For this test the nodes are:

    Client IP address: 10.10.1.1
    Broker IP address: 10.10.10.254

# Data Generation

Wireshark v1.12.7-0 running on the Windows client system traced the network activity. 

# Adverb Analysis - The Web Page

The [HelloWorld network trace web page](https://people.apache.org/~chug/blog/wordpress-03/helloworld.html)  is laid out with

* Page controls
* Connection data display control
* AMQP frames
* Decode legend
* Notes

In the HelloWorld example there is only one connection. In the real world a Wireshark trace might have dozens of simultaneous connections and then showing just one or two connections at a time using *Page control* and *Connection data display control* has more value. For this example just ignore those controls skip down to the *AMQP frames* section to see the protocol operation. 

# Adverb Analysis

Individual lines from the [HelloWorld source code](https://people.apache.org/~chug/blog/wordpress-01/helloworld.cs.html) are repeated in the following text so a reference to the source is not strictly necessary. The [HelloWorld network trace](https://people.apache.org/~chug/blog/wordpress-03/helloworld.html) for this discussion would be useful to examine for details of AMQP in action. 

## Open a Connection

Source code Line 14

    Connection connection = new Connection(brokerAddr);

generates several network frames. The client and broker first open a TCP connection. Then they negotiate an AMQP protocol version that the connection should use. That happens in trace Frames 819, 822 where they exchange AMQP init messages and agree to use AMQP version 1.0. 

Then the client and broker exchange **AMQP Open** performatives in Frames 837, 848.  Note that the broker returns Offered-Capabilities and Properties in Frame 848. These will be explained in another more advanced post.

## Begin a Session

Source code Line 15

    Session session = new Session(connection);

generates a pair of **AMQP Begin** performatives in Frames 851, 878 to create a session. In Frame 851 the client is proposing the session creation by sending 

    * Channel 0
    * Remote-Channel: null
     
This is a new session so the client has no remote channel. In Frame 878 the broker completes the session creation by sending 

    * Channel 0
    * Remote Channel: 0

Over this connection a new session has been created. The client handle 0 is paired with the broker handle 0.

In Frame 878 the broker also sends an **AMQP Flow** performative giving the client session-level credits. Session-level flow control and credits will be explained in another post.

## Attach Links

Source code Lines 17, 18

    SenderLink sender = 
        new SenderLink(session, "sender", address);
    ReceiverLink receiver = 
        new ReceiverLink(session, "receiver", address);

directly generate Frames 905 and 913. 

### Sender Link

The client wants to send to address "my\_queue" in the broker namespace. This is reflected in trace Frame 905. 

   * Attach performative
   * Link name: sender
   * Role: sender
   * Source: null
   * Target: my_queue

The broker responds by creating another link back to the client. This pair of links forms a full duplex channel over which the client and broker can exchange messages. Trace Frame 910 from the broker has the details of the broker's link creation:

   * Attach performative
   * Link name: sender
   * Role: receiver
   * Source: null
   * Target: my_queue

The names of the links do not affect the communications over them and are not used outside of the Attach performative. The client can choose a link name that is descriptive, a random GUID string, or blank. The only restriction is that the ordered tuple of (Source, Target, Name) be unique.

A small point of confusion here may be that the broker creates a receiver link named "sender". The A-MQ broker is simply mirroring the name of the link that it received from the client when it created the sender link.

Note in Frame 910 the broker is also sending an AMQP Flow performative granting link-level credits. In this case the broker is sending the client enough credit so that the client may have up to 1000 messages outstanding on this link.

### Receiver Link

The client's receiver link and the corresponding broker's sender link are created with a pattern similar to the client's sender link.

In Frame 913 the client creates the link with:

   * Attach performative
   * Link name: receiver
   * Role: receiver
   * Source: my_queue
   * Target: null

The broker responds with Frame 940.

   * Attach performative
   * Link name: receiver
   * Role: sender
   * Source: my_queue
   * Target: null

A difference to note from the sender link pair creation is that the client did not issue any credit to the broker's sender link. In this state even if the broker has messages available it will not send them. 

## Send a Message

Source code Line 21

    sender.Send(helloOut);

generates trace Frame 973 and an **AMQP Transfer** frame that sends a message to the broker.

In this example the client sends the message with property

    * Settled: false
 
 This means that the client will keep the message in it's outgoing message memory until the broker acknowledges it. Now the client still "owns" the message even though the message was successfully transferred over the wire to the broker.

## Transfer Ownership of Sent Message to Broker

In trace Frame 1004 the broker sends an **AMQP Disposition** performative that changes ownership of the message from the client to the broker. At this point the message has gone into the broker's queue *my\_queue* and the client forgets that the message ever existed. In AMQP terms the message is now *settled*.

## Receive a Message

Source code Line 23

    Message helloIn = receiver.Receive();

directly generates Frame 1113 and indirectly generates Frame 1118. In Frame 1113 the client issues enough credit so that the broker can send 200 messages on the receiver link. The broker has one message in the queue and sends it to the client in the **AMQP Transfer** performative in Frame 1118.

## Transfer ownership of Received Message to Client

Source code Line 24

    receiver.Accept(helloIn);

generated trace Frame 1123. The same message ownership that we just observed sending the message to the broker is not happening as the reply is transferred to the client. After receiving the **AMQP Disposition** performative the broker is free to remove the message from *my\_queue* and forget that the message ever existed.

If the client did not call Accept then the broker would still have the message in the queue.

## Closing the Links, Session, and Connection

Source code Lines 28..31

    receiver.Close();
    sender.Close();
    session.Close();
    connection.Close();

close the links, session, and connection in an orderly manner. 

 * Frames 1127, 1130: **AMQP Detach** closes receiver link
 * Frames 1147, 1148: **AMQP Detach** closes sender link
 * Frames 1153, 1154: **AMQP End** closes the session
 * Frames 1165, 1166: **AMQP Close** closes the connection

# Advanced Topics

AMQP 1.0 is a complete and powerful messaging protocol. Even a short tutorial like this touches on some parts of AMQP that are harder to explain. These are topics for upcoming blog posts:

* Broker capabilities: ANONYMOUS-RELAY
* Broker properties: topic-prefix, queue-prefix
* Session and Link credit
* Naming requirements
* Delivery settlement mode: at-least-once, at-most-once, exactly-once
* SASL user authentication

The HelloWorld example does not use AMQP.Net Lite advanced methods. Future posts should include AMQP.Net:

* Message and Application properties
* Async IO and Task usage
* AMQP addresses and message routing
* ContainerHost abstraction

Finally I have to admit that a few code changes to the source were required to contrive the accompanying Adverb trace. 

* The source code shows a connection to a broker at localhost but the trace shows the broker at 10.10.10.254. Wireshark doesn't capture traffic to localhost on a Windows system. Moving the broker to another system made this capture possible.
* Careful examination of the timestamps in the trace show that the frames are separated by hundreds of milliseconds. I deliberately injected delays into the code just to make the network trace pretty and understandable for an entry-level tutorial. Here is [the same trace without the network delays](https://people.apache.org/~chug/blog/wordpress-03/helloworld-3-nodelay.html). Note that the client fires off the Open, Begin, and Attach frames without waiting for any response from the broker and in Frame 1897 the broker sends six performatives back to the client. AMQP is designed to pipeline traffic and the AMQP.Net Lite client and A-MQ broker use pipelined messages to their advantage. Expect to see traffic patterns like this in your network.

# Conclusion

A trivial HelloWorld AMQP.Net Lite application generates instances of all AMQP performatives. Using tools to capture and display the AMQP frames are helpful to learning and using AMQP correctly. Project Adverb distills Wireshark AMQP traces into comprehensible, dynamic web pages.

Happy Messaging!
