/*
 * Copyright (c) 2002, 2012, Oracle and/or its affiliates. All rights reserved.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * This code is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 2 only, as
 * published by the Free Software Foundation.  Oracle designates this
 * particular file as subject to the "Classpath" exception as provided
 * by Oracle in the LICENSE file that accompanied this code.
 *
 * This code is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * version 2 for more details (a copy is included in the LICENSE file that
 * accompanied this code).
 *
 * You should have received a copy of the GNU General Public License version
 * 2 along with this work; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 * Please contact Oracle, 500 Oracle Parkway, Redwood Shores, CA 94065 USA
 * or visit www.oracle.com if you need additional information or have any
 * questions.
 */

package sun.net.dns;

import java.util.List;
import java.util.LinkedList;
import java.util.StringTokenizer;
import java.io.IOException;
// import cli.System.Net.NetworkInformation.IPAddressCollection;
// import cli.System.Net.NetworkInformation.IPInterfaceProperties;
// import cli.System.Net.NetworkInformation.NetworkInterface;

/*
 * An implementation of sun.net.ResolverConfiguration for Windows.
 */

public class ResolverConfigurationImpl
    extends ResolverConfiguration
{
    // Lock helds whilst loading configuration or checking
    private static Object lock = new Object();

    // Resolver options
    private final Options opts;

    // Addreses have changed
    private static boolean changed = false;

    // Time of last refresh.
    private static long lastRefresh = -1;

    // Cache timeout (120 seconds) - should be converted into property
    // or configured as preference in the future.
    private static final int TIMEOUT = 120000;

    // DNS suffix list and name servers populated by native method
    private static String os_searchlist;
    private static String os_nameservers;

    // Cached lists
    private static LinkedList<String> searchlist;
    private static LinkedList<String> nameservers;

    // Parse string that consists of token delimited by space or commas
    // and return LinkedHashMap
    private LinkedList<String> stringToList(String str) {
        LinkedList<String> ll = new LinkedList<>();

        // comma and space are valid delimites
        StringTokenizer st = new StringTokenizer(str, ", ");
        while (st.hasMoreTokens()) {
            String s = st.nextToken();
            if (!ll.contains(s)) {
                ll.add(s);
            }
        }
        return ll;
    }

    // Load DNS configuration from OS

    private void loadConfig() {
        // assert Thread.holdsLock(lock);

        // // if address have changed then DNS probably changed aswell;
        // // otherwise check if cached settings have expired.
        // //
        // if (changed) {
            // changed = false;
        // } else {
            // if (lastRefresh >= 0) {
                // long currTime = System.currentTimeMillis();
                // if ((currTime - lastRefresh) < TIMEOUT) {
                    // return;
                // }
            // }
        // }

        // // load DNS configuration, update timestamp, create
        // // new HashMaps from the loaded configuration
        // //
        // loadDNSconfig0();

        // lastRefresh = System.currentTimeMillis();
        // searchlist = stringToList(os_searchlist);
        // nameservers = stringToList(os_nameservers);
        // os_searchlist = null;                       // can be GC'ed
        // os_nameservers = null;
		
        throw new Error("Not implemented");
    }

    ResolverConfigurationImpl() {
        opts = new OptionsImpl();
    }

    @SuppressWarnings("unchecked") // clone()
    public List<String> searchlist() {
        synchronized (lock) {
            loadConfig();

            // List is mutable so return a shallow copy
            return (List<String>)searchlist.clone();
        }
    }

    @SuppressWarnings("unchecked") // clone()
    public List<String> nameservers() {
        synchronized (lock) {
            loadConfig();

            // List is mutable so return a shallow copy
            return (List<String>)nameservers.clone();
         }
    }

    public Options options() {
        return opts;
    }

    // --- Address Change Listener

    static class AddressChangeListener extends Thread {
        public void run() {
            for (;;) {
                // wait for configuration to change
                if (notifyAddrChange0() != 0)
                    return;
                synchronized (lock) {
                    changed = true;
                }
            }
        }
    }


    // --- Native methods --

    static void init0() {
    }

    static void loadDNSconfig0() {
	// String searchlist = "";
	// String nameservers = "";
	// for (NetworkInterface iface : NetworkInterface.GetAllNetworkInterfaces()) {
	    // IPInterfaceProperties props = iface.GetIPProperties();
	    // IPAddressCollection addresses = props.get_DnsAddresses();
	    // for (int i = 0; i < addresses.get_Count(); i++) {
	        // cli.System.Net.IPAddress addr = addresses.get_Item(i);
	        // // no IPv6 support
	        // if (addr.get_AddressFamily().Value == cli.System.Net.Sockets.AddressFamily.InterNetwork) {
                    // nameservers = strAppend(nameservers, addr.toString());
	        // }
	    // }
	    // try {
	        // if (false) throw new cli.System.PlatformNotSupportedException();
		// searchlist = strAppend(searchlist, props.get_DnsSuffix());
	    // }
	    // catch (cli.System.PlatformNotSupportedException _) {
	    // }
	// }
	// os_searchlist = searchlist;
	// os_nameservers = nameservers;
    // }
    
    // private static String strAppend(String s, String app) {
        // if (s.equals("")) {
            // return app;
        // }
        // if (app.equals("")) {
            // return s;
        // }
        // return s + " " + app;
		
        throw new Error("Not implemented");
    }

    static int notifyAddrChange0() {
        // TODO we could use System.Net.NetworkInformation.NetworkChange to detect changes
        return -1;
    }

    static {
        java.security.AccessController.doPrivileged(
            new java.security.PrivilegedAction<Void>() {
                public Void run() {
                    System.loadLibrary("net");
                    return null;
                }
            });
        init0();

        // start the address listener thread
        AddressChangeListener thr = new AddressChangeListener();
        thr.setDaemon(true);
        thr.start();
    }
}

/**
 * Implementation of {@link ResolverConfiguration.Options}
 */
class OptionsImpl extends ResolverConfiguration.Options {
}
