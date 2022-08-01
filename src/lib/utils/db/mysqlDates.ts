// Utilities for dealing with MySQL's datetime quirks

// Motivation: MySQL doesn't like the "2001-12-25T12:34:56.789Z" format; it wants strings *without time zones* for its
// datetime columns. So we need to parse incoming dates and convert them to Javascript Date objects, which the mysql2
// library knows how to pass to MySQL properly.

import { parseISO } from 'date-fns';

function parseDateColumns(data: Record<string, any>) {
    if (typeof data.created_on === 'string') {
        data.created_on = parseISO(data.created_on);
    }
    if (typeof data.updated_on === 'string') {
        data.updated_on = parseISO(data.updated_on);
    }
}

export function setDateColumnsForCreateWithoutUpdate(data: Record<string, any>) {
    parseDateColumns(data);
    if (!data.created_on) {
        data.created_on = new Date();
    }
}

export function setDateColumnsForCreate(data: Record<string, any>) {
    parseDateColumns(data);
    const now = new Date();
    if (!data.created_on) {
        data.created_on = now;
    }
    if (!data.updated_on) {
        data.updated_on = now;
    }
}

export function setDateColumnsForUpdate(data: Record<string, any>) {
    parseDateColumns(data);
    if (!data.updated_on) {
        data.updated_on = new Date();
    }
}
