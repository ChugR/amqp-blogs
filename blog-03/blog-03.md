This blog post exposes some of the details of the [AMQP protocol version 1.0](http://docs.oasis-open.org/amqp/core/v1.0/os/amqp-core-complete-v1.0-os.pdf). Code from the previous [HelloWorld example](https://people.apache.org/~chug/blog/wordpress-01/helloworld.cs.html) is traced by [Wireshark](https://www.wireshark.org/) to expose the over-the-wire protocol details. The trace is made comprehensible by [Adverb](https://github.com/ChugR/Adverb), my AMQP comprehension tool. 

HelloWorld is an unrealistically simple use of AMQP. Real world applications need any number of more advanced techniques. That said, HelloWorld generates instances of every AMQP performative. For folks new to AMQP seeing lines of C# code together with the over-the-wire protocol packets will speed the learning process. The goal here is to orient the reader and expose all the details of a successful AMQP protocol operation. 

# Test Setup

For this test the nodes involved are

    Client ip: 10.10.1.1
    Broker ip: 10.10.10.254

# Data Generation

Wireshark v1.12.7-0 running on the Windows client system traced the network activity. The raw data in a pcapng file was fed to an Adverb server to produce the annotated [HelloWorld network trace](https://people.apache.org/~chug/blog/wordpress-03/helloworld.html)

# Introduction to the Adverb Web Page

The network trace web page was generated from a  pcapng trace file using my  AMQP comprehension tool. Wireshark is great, and Adverb absolutely requires it for packet decoding. But it's easy to get lost in a sea of Wireshark details while analyzing network traffic. Adverb presents you with a web page that has metadata, sorting, and display controls all geared to understanding AMQP.

The Adverb web page is laid out with

* Page controls
* Connection data display control
* AMQP frames
* Decode legend
* Notes

In the HelloWorld case there is only one connection. In the real world a Wireshark trace might have dozens of simultaneous connections. Then showing just one or two connections at a time has more value. Ignore those controls for this case.

Skip down to the AMQP frames section to see the protocol operation. 

The Decode legend and Notes describe how to use the interactive controls and what data is summarized for each protocol frame.

# Hello World Analysis
