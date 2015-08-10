# AMQP.Net Lite Client, A-MQ Broker, and TLS/SSL

In this post I'm going to show how to encrypt your communications between the broker and the client. 

This is not a formula for securing a large, scalable, public installation. Rather it will demonstrate the concepts and give you some recipes you can use in your test setups to show that your broker and clients are using security credentials properly.

## Overview

There are two levels of authentication that can be performed:

 * [TLS Handshake](https://technet.microsoft.com/en-us/library/Cc783349\(v=WS10\).aspx)
 * TLS Handshake with Client Certificate Request

### TLS Handshake

TLS provides an authenticated and secured communications channel. For simple TLS the broker must present a certificate that is signed by a certificate authority (CA) that the client trusts.

### TLS Handshake with Client Certificate Request

The broker must always send a certificate to the client to start the secure channel with the TLS Handshake. Using a client certificate request the broker can demand that the client authenticate itself by sending a certificate to the broker. The client's identity is securely embedded in the client certificate and by carefully controlling who gets the client certificates a system administrator may control who gets access to resources in the A-MQ broker. 

This is useful for messaging environments such as a banking network where the broker must confirm the identity of the client with stronger authentication than just a username and password. 

## Security Providers

The AMQP.Net Lite Client and the Red Hat JBoss A-MQ Broker use different security providers for creating and managing SSL/TLS connections.

 * AMQP.Net Lite Client uses [SChannel](https://msdn.microsoft.com/en-us/library/windows/desktop/ms678421%28v=vs.85%29.aspx)
 * Red Hat JBoss A-MQ Broker uses [javax.net.ssl](http://docs.oracle.com/cd/B19306_01/java.102/b14355/sslthin.htm#BABCBGDB).

In either case we must configure the security provider with the proper certificates before the provider can set up secure SSL/TLS communication channels.

## Certificate Generation Tools

In this example we are going to generate all the certificates locally using a publicly available tool. There are plenty of certificate generation tools from which to choose:

 * OpenSSL
 * Java Keytool
 * IBM KeyMan
 * Microsoft Makecert
 * Portecle
 * and more ...


For this article I choose to use [Java Keytool](https://docs.oracle.com/javase/8/docs/technotes/tools/unix/keytool.html) from [Oracle](http://www.oracle.com/technetwork/java/javase/downloads/index.html) to run on a Windows host. To get *keytool*, install Java Platform, Standard Edition (Java SE) and add the kit to your execution environment. These settings add version jdk1.7.0_79 to a process environment:

    set java_home=c:\program files\java\jdk1.7.0_79
    set      path=%path%;%java_home%\bin

## Generate a set of certificates

### Overview

The script generates all the certificates needed for TLS Handshake and Client Certificate exchange.
The steps to perform are:

 * Create a private, local Certificate Authority (the 'CA')
 * Create a key pair for the broker and sign it with CA
 * Import CA and broker certificates into broker keystore
 * Create a key pair for the client and sign it with CA
 * Import CA and client certificates into client keystore
 * Export client private key and certificate trust chain into a Personal Information Exchange
 * Create a trust store for the broker, import CA certificate into it

### Configuration script changes required for your setup

In this script you need to adjust some variables to match the environment in which the scripts will be used. 

 * CERT_LOC The folder where all the certificates are generated and stored.
 * CA_CN The name of the self-signed certificate authority this script creates. There are no limits on this value but use some common sense and don't create a name that conflicts with any public certificate authority.
 * BROKER_CN This name must be that public hostname of the broker system. This is the name in the connection URL that the client uses to access the broker.
 * CLIENT_CN This is the client's username used in the client certificate.

For more complicated installations you may want more descriptive file names. The file path suffixes (.keystore, .truststore, .crt, .csr, .p12) are common for the content illustrated here.

### Certificate generation script

Generate your certificate files with [this script](https://people.apache.org/~chug/blog/blog-02/gen-win-ssl-certs.bat.html)

### Distribute Certificates

On the broker system the broker configuration references files

    broker-jks.keystore
    broker-jks.truststore

On client systems that use only the TLS Handshake the client needs

    ca.crt

On client systems that use TLS Handshake and Client Certificate the client needs

    ca.crt
    client.crt
    client.p12

## Configuring the Broker

### Referencing the keystore and truststore files

There is no special installation required for the certificate files. They must be readable during broker start up.

Assume that you have run the generation script in the broker's root folder. The certificates will be in folder .\keys. In the broker configuration file the key stores are loaded by adding these lines to file *conf\activemq.xml*.

    <sslContext>
      <sslContext
        keyStore="${activemq.conf}/../keys/broker-jks.keystore" keyStorePassword="password"
        trustStore="${activemq.conf}/../keys/broker-jks.truststore" trustStorePassword="password"/>
    </sslContext>

### Creating the AMQPS TLS Transport Connector for TLS Handshake Only

In file *conf/activemq.xml* add the TLS/SSL AMQP connector

    <transportConnector name="amqp" uri="amqp://0.0.0.0:5672?maximumConnections=1000&amp;wireFormat.maxFrameSize=104857600"/>
    <transportConnector name="amqps" uri="amqp+ssl://0.0.0.0:5671?maximumConnections=1000&amp;wireFormat.maxFrameSize=104857600"/>


### Creating the AMQPS TLS Transport Connector for TLS Handshake and Client Certificate

In file *conf\activemq.xml* add a qualifier to the AMQPS transport connector

    <transportConnector name="amqps" uri="amqp+ssl://0.0.0.0:5671?maximumConnections=1000&amp;wireFormat.maxFrameSize=104857600&amp;needClientAuth=true"/>

Restart the broker. The TLS transport connector should be listening on port 5671.

## Configuring the Client

### Install the CA certificate

Client TLS Handshake is enabled by installing file *ca.crt* in the system's Trusted Root Certification Authorities store.

 * From an Administrator command prompt run the MMC Certificate Manager plugin: **certmgr.msc**
 * Expand the *Trusted Root Certification Authorities* folder on the left to expose *Certificates*
 * Right click *Certificates* and select *All Tasks -> Import...*
 * Click Next
 * Browse to select file *ca.crt*
 * Click Next
 * Select *Place all certificates in the following store*
 * Select Certificate store : *Trusted Root Certification Authorities*
 * Click Next
 * Click Finish

The certificate issued by *reallly-trust-me.org* should appear in the *Trusted Root Certification Authorities\Certificates* list.

### Use TLS Handshake Only

The client system is now configured to connect to the broker using TLS Handshake only. The client's source code must be adjusted to select the TLS transport channel. In *HelloWorld_simple.cs* the broker URL uses an unsecured channel:

    string broker = "amqp://guest:password@10.10.10.254:5672";

The client can connect to the secured channel by changing the scheme to *amqps* and the port number to *5671*

    string broker = "amqps://guest:password@10.10.10.254:5671";

### Use TLS Handshake and Client Certificates

In order to use TLS and client certficates then the certificates with the client's private keys must be imported into the proper certificate store on the client system.

 * From an Administrator command prompt run the MMC Certificate Manager plugin: **certmgr.msc**
 * Expand the *Personal* folder on the left to expose *Certificates*
 * Right click *Certificates* and select *All Tasks -> Import...*
 * Click Next
 * Click Browse
 * In the file type pulldown select *Personal Information Exchange (\*.pfx;\*.p12)*
 * Select file *client.p12* and press Open
 * Click Next
 * Type in the password for the private key: **password**. Accept default import options.
 * Click Next
 * Select *Place all certificates in the following store*
 * Select Certificate store : *Personal*
 * Click Next
 * Click Finish

### Hello World Example Using Client Certificates

Before a client will return a certificate to the broker, the AMQP.Net Lite library must be told which certificates to use. In the current release of AMQP.Net Lite this may be done using the async task methods shown in the following example. This example is available in Red Hat JBoss A-MQ AMQP.Net Lite Client SDK. See file [HelloWorld-client-certs.cs](https://people.apache.org/~chug/blog/wordpress-02/HelloWorld-client-certs.cs.html).

In this example the client certificate file *client.crt* is added to the list of certificates to be used during SChannel connection startup. 

    factory.SSL.ClientCertificates.Add(
        X509Certificate.CreateFromCertFile(certfile));

## Conclusion

With the proper certificates AMQP.Net Lite clients may use TLS Handshake encrypted channels when communicating with Red Hat JBoss A-MQ brokers. Advanced authentication may be performed with TLS Client Certificates.

Happy Messaging!
