
/*
 *  Licensed to the Apache Software Foundation (ASF) under one or more
 *  contributor license agreements.  See the NOTICE file distributed with
 *  this work for additional information regarding copyright ownership.
 *  The ASF licenses this file to You under the Apache License, Version 2.0
 *  (the "License"); you may not use this file except in compliance with
 *  the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

package com.codename1.java0.util;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
/**
 * TimeZone represents a time zone offset, and also figures out daylight savings.
 * Typically, you get a TimeZone using getDefault which creates a TimeZone based on the time zone where the program is running. For example, for a program running in Japan, getDefault creates a TimeZone object based on Japanese Standard Time.
 * You can also get a TimeZone using getTimeZone along with a time zone ID. For instance, the time zone ID for the Pacific Standard Time zone is "PST". So, you can get a PST TimeZone object with:
 * This class is a pure subset of the java.util.TimeZone class in JDK 1.3.
 * The only time zone ID that is required to be supported is "GMT".
 * Apart from the methods and variables being subset, the semantics of the getTimeZone() method may also be subset: custom IDs such as "GMT-8:00" are not required to be supported.
 * Version: CLDC 1.1 02/01/2002 (Based on JDK 1.3) See Also:Calendar, Date
 */
public abstract class TimeZone extends java.util.TimeZone {
    /**
     * The short display name style, such as {@code PDT}. Requests for this
     * style may yield GMT offsets like {@code GMT-08:00}.
     */
    public static final int SHORT = 0;
    
    /**
     * The long display name style, such as {@code Pacific Daylight Time}.
     * Requests for this style may yield GMT offsets like {@code GMT-08:00}.
     */
    public static final int LONG = 1;
    
    static final TimeZone GMT = new SimpleTimeZone(0, "GMT"); // Greenwich Mean Time
    
    private static TimeZone defaultTimeZone;
    
    private String ID;

    public TimeZone(){         
    }

    public void setID(String id) {
        ID = id;
    }
    
    /**
     * Gets all the available IDs supported.
     */
    public static java.lang.String[] getAvailableIDs(){
        String i = getTimezoneId();
        if(i.equals("GMT")) {
            return new String[] {"GMT"};//ZoneInfoDB.getAvailableIDs();
        } else {
            return new String[] {"GMT", i};
        }
    }
    
    public static java.lang.String[] getAvailableIDs(int offset) {
        List<String> out = new ArrayList<String>();
        for (String id : getAvailableIDs()) {
            if (getTimezoneRawOffset(id) == offset ) {
                out.add(id);
            }
        }
        return out.toArray(new String[out.size()]);
    }

    private static String getTimezoneId() { 
        return cli.IKVM.Internal.RuntimeReflectionHelper.get_Instance().getTimezoneId();
    }
    private static  int getTimezoneOffset(String name, int year, int month, int day, int timeOfDayMillis){
        return cli.IKVM.Internal.RuntimeReflectionHelper.get_Instance().getTimezoneOffset(name, year, month, day, timeOfDayMillis);
    }
    private static  int getTimezoneRawOffset(String name) {
        return cli.IKVM.Internal.RuntimeReflectionHelper.get_Instance().getTimezoneRawOffset(name);
    }
    private static  boolean isTimezoneDST(String name, long millis) {
        return cli.IKVM.Internal.RuntimeReflectionHelper.get_Instance().isTimezoneDST(name, millis);
    }

    /**
     * Gets the default TimeZone for this host. The source of the default TimeZone may vary with implementation.
     */
    public static java.util.TimeZone getDefault(){
        if (defaultTimeZone == null) {
            final String tzone = getTimezoneId();
            defaultTimeZone = new TimeZone() {
                @Override
                public int getOffset(int era, int year, int month, int day, int dayOfWeek, int timeOfDayMillis) {
                    return getTimezoneOffset(tzone, year, month + 1, day, timeOfDayMillis);
                }

                @Override
                public int getRawOffset() {
                    return getTimezoneRawOffset(tzone);
                }

                public boolean inDaylightTime(Date time) {
                    return isTimezoneDST(tzone, time.getTime());
                }

                @Override
                public boolean useDaylightTime() {
                    return true;
                }
            };
            defaultTimeZone.ID = tzone;
        }
        return defaultTimeZone;
    }

    public int getDSTSavings() {
        return useDaylightTime() ? 3600000 : 0;
    }
    
    public void setRawOffset(int off) {
        
    }
    
    public boolean inDaylightTime(Date time) {
        return false;
    }
    
    /**
     * Gets the ID of this time zone.
     */
    public java.lang.String getID(){
        return ID;
    }

    /**
     * Gets offset, for current date, modified in case of daylight savings. This is the offset to add *to* GMT to get local time. Gets the time zone offset, for current date, modified in case of daylight savings. This is the offset to add *to* GMT to get local time. Assume that the start and end month are distinct. This method may return incorrect results for rules that start at the end of February (e.g., last Sunday in February) or the beginning of March (e.g., March 1).
     */
    public abstract int getOffset(int era, int year, int month, int day, int dayOfWeek, int millis);

    /**
     * Gets the GMT offset for this time zone.
     */
    public abstract int getRawOffset();

    /**
     * Gets the TimeZone for the given ID.
     */
    public static java.util.TimeZone getTimeZone(java.lang.String ID){
        // TODO
        return getDefault();
    }

    /**
     * Queries if this time zone uses Daylight Savings Time.
     */
    public abstract boolean useDaylightTime();

}
