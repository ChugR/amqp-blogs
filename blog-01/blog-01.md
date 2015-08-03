# Red Hat JBoss AMQP.Net Lite Client - Hello World!

AMQP.Net Lite is a lightweight AMQP 1.0 library for .NET. 

This article describes how to install and use the library packaged in the *Red Hat JBoss A-MQ .NET Lite SDK*. The example connects to services in a [Red Hat JBoss A-MQ broker](https://access.redhat.com/documentation/en-US/Red_Hat_JBoss_A-MQ/6.2/index.html).

## Download/Install AMQP.Net Lite

AMQP.Net Lite is available from a number of sources. The one you pick depends on your needs.

* Red Hat JBoss A-MQ AMQP.Net Lite Client SDK
* NuGet
* Git

### Red Hat JBoss A-MQ AMQP.Net Lite Client SDK

As part of *Red Hat JBoss A-MQ 6.2* Red Hat supplies an SDK for AMQP.Net Lite. The SDK has precompiled binary library files for use on a Windows desktop system running Visual Studio 2012 or Visual Studio 2013, AMQP.Net Lite API documentation, and a set of example programs. This is an ideal environment for getting started with .NET and AMQP.

#### Download

Download the SDK from [Red Hat JBoss A-MQ - Downloads](http://www.jboss.org/products/amq/download/). On the download page choose *.NET Lite SDK*. This will result in downloading a single file: *amqpnetlite-sdk-1.1.0.2.zip*.

#### Install

Installing AMQP.Net Lite consists of unzipping the distribution file. This may be done using Windows Explorer or by a third party utility such as [7-Zip](http://www.7-zip.org/). 

A typical unzipped installation will look like this:

    W:\1.1.0.2>dir
     Directory of W:\1.1.0.2
    
    06/18/2015  03:58 PM    <DIR>          amqpnetlite
    06/03/2015  01:00 PM         1,020,150 amqpnetlite-sdk-1.1.0.2.zip

Inside the *amqpnetlite* folder you will find the Visual Studio 2012 and 2013 solution files and the other files that make up the kit.

    W:\1.1.0.2>dir amqpnetlite
     Directory of W:\1.1.0.2\amqpnetlite
    
    05/27/2015  01:20 PM            12,757 amqp-vs2012.sln
    05/27/2015  01:20 PM            12,837 amqp.sln
    06/11/2015  04:53 PM    <DIR>          bin
    06/11/2015  04:53 PM    <DIR>          doc
    06/11/2015  04:53 PM    <DIR>          Examples
    05/27/2015  01:20 PM             3,763 README-amqpnetlite.txt

### NuGet

AMQP.Net Lite is available through the Package Manager Console integrated with Visual Studio. Users will find this method of installation very convenient but the process does not install examples nor supporting documentation. I'd recommend the NuGet package for experienced users who just need the runtime library bits and don't need help getting started.

To install using NuGet issue the following command in the Package Manager Console

    Install-Package AMQPNetLite

### Git from upstream

The upstream project is open source. Visit the [home page](http://amqpnetlite.codeplex.com).

The source code is available at

    git clone https://git01.codeplex.com/amqpnetlite

The project supports a variety of .NET Frameworks:

 - .NET 3.5
 - .NET 4.0
 - .NET Core
 - Windows Phone
 - Windows RT
 - Windows Store
 - Compact Framework
 - Micro Framework

When you try to build a fresh source from upstream is it unlikely that your system will have all of the .NET packages required to support everything AMQP.Net Lite offers. While loading the solution file Visual Studio emits a series of errors as it removes projects that can not compile on your system now. Not to worry, though, as projects reappear when the underlying support for them is installed.

The upstream project also includes dozens of self tests that illustrate how things are supposed to work. These are a great reference.

## AMQP 1.0

AMQP.Net Lite uses only [AMQP version 1.0](http://docs.oasis-open.org/amqp/core/v1.0/os/amqp-core-complete-v1.0-os.pdf) and interoperates with a system such as the JBoss A-MQ broker that also uses AMQP 1.0.

A brief introduction to the AMQP 1.0 messaging protocol is in order. The AMQP.Net Lite programming API closely models the structures defined by AMQP 1.0. If one is familiar with AMQP 1.0 then one understands most of the relationships in the messaging aspects of AMQP.Net Lite.

### Connection

A Connection is a full-duplex, reliably ordered sequence of frames, or units of work, carried over the communication wire.

 - AMQP requires guarantees similar to what TCP or SCTP provide for byte streams.
 - A single AMQP Connection is established for each underlying transport (TCP) connection.

### Session

Within a Connection a Session is a grouping of some number of independent unidirectional channels.

 - Sessions provide sequencing and flow control at the frame transfer level.
 - A Connection may contain any number of Sessions.

### Link

Within a Session a Link is a unidirectional route between a *source* and a *target*, one at each end of the Connection.

- Links are the paths over which messages are transferred. 
- Links are unidirectional. 
- Pairs of links are bound to create full duplex communication channels between endpoints. 
- A single Session may be associated with any number of Links. 
- Links provide flow control at the message transfer level.

From a client's point of view the source and target are names of the terminus objects in the connection peer's namespace. The SenderLink *target* name and  ReceiverLink *source* name are the names of queues or topics in the broker. 

The example code sends and receives messages to and from a queue named *my\_queue*. The JBoss A-MQ broker may autocreate this queue if it does not already exist. Note that the JBoss A-MQ broker may be configured with a security setting that restricts queue autocreation. With that restriction then *my\_queue* must be created administratively before the example will work.

### Messaging

AMQP 1.0 Messaging has a rich and well structured design to suit virtually any message use case. In the example only a simple message consisting of a text string is used. Real world applications are typically much more demanding and will use more advanced messaging properties and capabilities.

## Hello World Example

Let's get going with HelloWorld. This example is file *HelloWorld-simple.cs* in the SDK.

### Source Code
     
<!-- HTML generated using hilite.me --><div style="background: #ffffff; overflow:auto;width:auto;border:solid gray;border-width:.1em .1em .1em .8em;padding:.2em .6em;"><table><tr><td><pre style="margin: 0; line-height: 125%"> 1
 2
 3
 4
 5
 6
 7
 8
 9
10
11
12
13
14
15
16
17
18
19
20
21
22
23
24
25
26
27
28
29
30
31
32
33
34</pre></td><td><pre style="margin: 0; line-height: 125%"><span style="color: #008800; font-weight: bold">using</span> <span style="color: #0e84b5; font-weight: bold">System</span>;
<span style="color: #008800; font-weight: bold">using</span> <span style="color: #0e84b5; font-weight: bold">Amqp</span>;

<span style="color: #008800; font-weight: bold">namespace</span> <span style="color: #0e84b5; font-weight: bold">HelloWorld</span>
{
  <span style="color: #008800; font-weight: bold">class</span> <span style="color: #BB0066; font-weight: bold">HelloWorld</span>
  {
    <span style="color: #008800; font-weight: bold">static</span> <span style="color: #008800; font-weight: bold">void</span> <span style="color: #0066BB; font-weight: bold">Main</span>(<span style="color: #333399; font-weight: bold">string</span>[] args)
    {
      <span style="color: #333399; font-weight: bold">string</span> brokerUrl = <span style="background-color: #fff0f0">&quot;amqp://localhost:5672&quot;</span>;
      <span style="color: #333399; font-weight: bold">string</span> address   = <span style="background-color: #fff0f0">&quot;my_queue&quot;</span>;

      Address    brokerAddr = <span style="color: #008800; font-weight: bold">new</span> Address(brokerUrl);
      Connection connection = <span style="color: #008800; font-weight: bold">new</span> Connection(brokerAddr);
      Session    session    = <span style="color: #008800; font-weight: bold">new</span> Session(connection);

      SenderLink   sender   = <span style="color: #008800; font-weight: bold">new</span>   SenderLink(session, <span style="background-color: #fff0f0">&quot;helloworld-sender&quot;</span>,   address);
      ReceiverLink receiver = <span style="color: #008800; font-weight: bold">new</span> ReceiverLink(session, <span style="background-color: #fff0f0">&quot;helloworld-receiver&quot;</span>, address);

      Message helloOut = <span style="color: #008800; font-weight: bold">new</span> Message(<span style="background-color: #fff0f0">&quot;Hello World!&quot;</span>);
      sender.Send(helloOut);

      Message helloIn = receiver.Receive();
      receiver.Accept(helloIn);

      Console.WriteLine(helloIn.Body.ToString());

      receiver.Close();
      sender.Close();
      session.Close();
      connection.Close();
    }
  }
}
</pre></td></tr></table></div>

### Code Notes

 * Line 2 The *using* directive enables unqualified use of the AMQP.Net Lite objects in the *AMQP* namespace.
 * Line 10 contains the URL specifying the network address of the A-MQ broker to which this connection is directed. The details of the URL format are explained below.
 * Line 11 has the name of the resource in the broker to which and from which links will be created.
 * Line 13 creates an Address object that holds the parsed URL.
 * Line 14 opens the connection:
     * A TCP connection is opened
     * AMQP Version information is exchanged and accepted
     * **AMQP Open** performatives are exchanged to create an AMQP Connection and to define Connection-level capabilities and properties.
 * Line 15 creates a session:
     * **AMQP Begin** performatives are exchanged to create an AMQP Session and to define Session-level values and limits.
 * Lines 17 and 18 create links to the resource in the broker.
     * **AMQP Attach** performatives are exchanged to create AMQP Links and to define Link-level names, values, and properties.
     * The broker issues **AMQP Flow** performatives to define credits that allow the client to send messages.
 * Line 20 creates a message holding a simple text string
 * Line 21 sends the message on the sender link
     * The client sends an **AMQP Transfer** performative containing the original message.
     * The broker sends an **AMQP Disposition** performative acknowledging and assuming ownership of the message. The broker puts the message into the *my\_queue* queue.
 * Line 23 receives a message on the receiver link.
     * The broker sends an **AMQP Transfer** performative containing the reply message.
     * Note: The receiver will time out and throw an exception if no message is received
     * The client issues an **AMQP Flow** performative to replenish the broker's credits for sending to the reply link.
 * Line 24 The receiver accepts the message.
     * The client sends an **AMQP Disposition** performative acknowledging and assuming ownership of the message. The broker removes the message from the *my\_queue* queue.
     * Note: if the client did not accept the message then the message would remain on the broker queue.
 * Line 25 prints "Hello World!"
 * Lines 28 and 29 close the receiver and sender links
     * **AMQP Detach** performatives are exchanged to dispose of the links.
 * Line 30 closes the session
     * **AMQP End** performatives are exchanged to dispose of the session.
 * Line 31 closes the connection and releases internal resources related to it
     * The client and broker exchange **AMQP Close** performatives to dispose of the connection.
     * The TCP connection between the client and broker is torn down.

#### AMQP.Net Lite Connection Specification

AMQP.Net Lite accepts a formatted [URL](https://en.wikipedia.org/wiki/Uniform_resource_locator) to specify the peer address.

    amqp[s] :// [user:[password]@] domain[:port] [/path]
    
 1. The scheme 
     - **amqp** specifies an unencrytped connection
     - **amqps** specifies an encrypted connection.
 2. **user** and **password** are optional credentials. When specified AMQP.Net Lite uses them during the [SASL](https://en.wikipedia.org/wiki/Simple_Authentication_and_Security_Layer) negotiation phase of AMQP connection establishment.
 3. **domain** is the domain name or literal IP numeric address of the target peer system.
 4. **port** number, given in decimal, is optional. If omitted, the default for the scheme is used: 
     - 5672 for amqp
     - 5671 for amqps
 5. **path** is part of the parsed Address class but is not used as part of the connection.

## Conclusion

Using AMQP.Net Lite it is easy to get going in the AMQP messaging space. With AMQP you can operate with Red Hat A-MQ brokers and with services in the Azure cloud with equal ease.

In a future blog post I want to share how to secure your AMQP communications with TLS/SLL. 
 
Happy Messaging!
