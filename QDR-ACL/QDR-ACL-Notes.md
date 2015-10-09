# Qpid Dispatch Router (QDR) ACL Notes
 #2015-10-09

## Overview

This page describes how an ACL scheme may be implemented in QDR. These are informal notes hashing out concepts and ideas before formal specification and coding.

### Policy Authority (PA)

QDR may be configured with an ACL Policy Authority (PA).
When so configured QDR asks PA for approval of every connection.

* If PA disapproves then that connection is denied.
* If PA approves then the connection is allowed and
  PA supplies a policy name to be applied to further connection activity.
* PA internals are TBD

### Policy

* A policy is identified by a name and a version.
* A policy holds rules limiting activity over a single connection. A policy has small scope.
* A single policy may be applied to multiple connections. Policies are reusable.
* Activity on a connection is controlled by a single policy. Policy rule application may be confused if multiple rule sets are applied in random order so only a single policy set per connection is allowed.
* Policy content and versioning is maintained outside QDR.
* QDR detects policy version changes and updates local copies internally. QDR maintains a local cache of policies.
* Policy file format is TBD
* Policy versioning and QDR cache update details are TBD

## Policy Event Processing

This section describes ACL processing associated with QDR network events

### Event: TCP connect
* **allow**.  ACL is not involved in connection setup

### Event: SSL
* **allow**.  ACL is not involved in connection setup

### Event: AMQP TLS 
* **allow**. ACL is not involved in AMQP Handshake

### Event: AMQP SASL
* **allow**. ACL is not involved in AMQP Handshake

### Event: AMQP AMQP
* **allow**. ACL is not involved in AMQP Handshake

### Event: AMQP Open
* **Intercept**  ACL intercepts the Open performative. The Open is not forwarded into the QDR network until the connection is approved by PA.

----------

		A request is sent to PA. It includes
     	* User host IP
	    * User negotiated/SASL username
		* QDR name
		* QDR port
		* AMQP Open frame info

----------

	    A reply is received from PA.
	    * "deny"
	       * Close connection sending canned Open/Close back to originating connection.
	    * "allow"
		   * Reply includes policy name.
           * Versioned policy is fetched from PA and applied to this connection.
           * Connection stalls until local policy cache is updated
           * When policy is in place then client Open performative is forwarded to QDR network.

### Event: AMQP Begin
* forwarded normally

### Event: AMQP Attach
**Intercept** The Attach performative is queried against the policy in force for the current connection. 

* If **denied** then a canned Attach/Detach is sent back to the originator thus denying the link creation. The user's Attach is not forwarded into the QDR network.
* If **allowed** then the user's Attach is forwarded into the QDR network. This link is allowed to proceed through its normal lifetime with no further checks or denials by ACL.

### Events: AMQP Flow, Transfer, Disposition, Detach, End, Close
* forwarded normally