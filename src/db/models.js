import { Model } from 'objection';
import { withoutKey } from '$utils/withoutKey';
import { renameKey } from '$utils/renameKey';
import { applyAll } from '$utils/applyAll';
import { setDateColumnsForCreate, setDateColumnsForCreateWithoutUpdate, setDateColumnsForUpdate } from '$utils/db/mysqlDates';
import { hashPasswordForStorage } from '$utils/db/passwords';
import { withOnlyKeys } from '$utils/withOnlyKeys';

// Models for Membership, Project and User must be in same file to avoid circular imports

class Membership extends Model {
    static tableName = 'members';
    static relationMappings = () => ({
        project: {
            relation: Model.HasOneRelation,
            modelClass: Project,
            join: {
                from: 'members.project_id',
                to: 'projects.id'
            }
        },
        role: {
            relation: Model.HasOneThroughRelation,
            modelClass: Role,
            join: {
                from: 'members.id',
                through: {
                    from: 'member_roles.member_id',
                    to: 'member_roles.role_id'
                },
                to: 'roles.id'
            }
        },
        user: {
            relation: Model.HasOneRelation,
            modelClass: User,
            join: {
                from: 'members.user_id',
                to: 'users.id'
            }
        }
    });
    $beforeInsert(opt, context) {
        super.$beforeInsert(opt, context);
        setDateColumnsForCreateWithoutUpdate(this);
    }
}

class MemberRole extends Model {
    // Only used for advanced membership manipulation
    static tableName = 'member_roles';
    static relationMappings = () => ({
        role: {
            relation: Model.HasOneRelation,
            modelClass: Role,
            join: {
                from: 'member_roles.role_id',
                to: 'roles.id'
            }
        },
        user: {
            relation: Model.HasOneThroughRelation,
            modelClass: User,
            join: {
                from: 'member_roles.member_id',
                through: {
                    from: 'members.id',
                    to: 'members.user_id'
                },
                to: 'users.id'
            }
        }
    });
}

class Project extends Model {
    static tableName = 'projects';
    static relationMappings = () => ({
        members: {
            relation: Model.HasManyRelation,
            modelClass: Membership,
            join: {
                from: 'projects.id',
                to: 'members.project_id',
            }
        },
        memberRoles: {
            relation: Model.HasManyRelation,
            modelClass: MemberRole,
            join: {
                from: 'projects.id',
                through: {
                    from: 'members.project_id',
                    to: 'members.id'
                },
                to: 'member_roles.member_id'
            }
        },
    });
    static jsonSchema = {
        type: 'object',
        required: ['identifier', 'name'],
        properties: {
            id: { type: 'integer' },
            name: { type: 'string', minLength: 1, maxLength: 255 },
            description: { type: 'string' },
        }
    };
    $formatJson(json) {
        // Called when we're about to return JSON to the outside world
        json = super.$formatJson(json);
        const result = applyAll(json,
            renameKey('identifier', 'projectCode'),
            withOnlyKeys(['projectCode', 'name', 'description', 'members']),
        );
        return result;
    }
    $parseJson(json) {
        // Called when we've received JSON from the outside world
        json = super.$parseJson(json);
        const result = applyAll(json,
            renameKey('projectCode', 'identifier')
        );
        return result;
    }
    $beforeInsert(opt, context) {
        super.$beforeInsert(opt, context);
        setDateColumnsForCreate(this);
    }
    $beforeUpdate(opt, context) {
        super.$beforeUpdate(opt, context);
        setDateColumnsForUpdate(this);
    }
}

class Email extends Model {
    static tableName = 'email_addresses';
    static relationMappings = () => ({
        user: {
            relation: Model.BelongsToOneRelation,
            modelClass: User,
            join: {
                from: 'email_addresses.user_id',
                to: 'users.id'
            }
        }
    });
    $beforeInsert(opt, context) {
        super.$beforeInsert(opt, context);
        setDateColumnsForCreate(this);
    }
    $beforeUpdate(opt, context) {
        super.$beforeUpdate(opt, context);
        setDateColumnsForUpdate(this);
    }
}

class User extends Model {
    static tableName = 'users';
    static relationMappings = () => ({
        projects: {
            relation: Model.ManyToManyRelation,
            modelClass: Project,
            join: {
                from: 'users.id',
                through: {
                    from: 'members.user_id',
                    to: 'members.project_id'
                },
                to: 'projects.id'
            }
        },
        memberships: {
            relation: Model.HasManyRelation,
            modelClass: Membership,
            join: {
                from: 'users.id',
                to: 'members.user_id'
            }
        },
        emails: {
            relation: Model.HasOneRelation,
            modelClass: Email,
            join: {
                from: 'users.id',
                to: 'email_addresses.user_id'
            }
        },
    });
    static jsonSchema = () => ({
        type: 'object',
        required: ['login', 'name'],
        properties: {
            id: { type: 'integer' },
            name: { type: 'string', minLength: 1, maxLength: 255 },
            description: { type: 'string' },
        }
    });
    $formatJson(json) {
        // Called when we're about to return JSON to the outside world
        json = super.$formatJson(json);
        return applyAll(json,
            withoutKey('hashed_password'),
            withoutKey('salt'),
            withoutKey('password'),  // Shouldn't happen, but let's be extra cautious
            renameKey('login', 'username'),
            withOnlyKeys(['username', 'firstname', 'lastname', 'admin', 'language']),
        );
    }
    $parseJson(json) {
        // Called when we've received JSON from the outside world
        json = super.$parseJson(json);
        const result = applyAll(json,
            renameKey('username', 'login')
        );
        return result;
    }
    $beforeInsert(opt, context) {
        super.$beforeInsert(opt, context);
        setDateColumnsForCreate(this);
        if (Object.prototype.hasOwnProperty.call(this, 'password')) {
            const { hashed_password, salt } = hashPasswordForStorage(this.password, undefined);
            this.hashed_password = hashed_password;
            this.salt = salt;
            delete this.password;
        }
    }
    $beforeUpdate(opt, context) {
        super.$beforeUpdate(opt, context);
        setDateColumnsForUpdate(this);
        if (Object.prototype.hasOwnProperty.call(this, 'password')) {
            if (this.password) { // Empty password in incoming JSON will leave existing password unchanged
                const { hashed_password, salt } = hashPasswordForStorage(this.password, undefined);
                this.hashed_password = hashed_password;
                this.salt = salt;
            }
            delete this.password;
        }
    }
}

class Role extends Model {
    static tableName = 'roles';
}

// Hardcoded role IDs for manager and contributor roles
// Must update these if the DB ever gets updated
const managerRoleId = 3;
const contributorRoleId = 4;
const techSupportRoleId = 6;

const defaultRoleName = 'Contributor';
const defaultRoleId = contributorRoleId;

const projectStatus = {
    // Values copied from Redmine source
    active: 1,
    closed: 5,
    archived: 9,
}

const userStatus = {
    // Values copied from Redmine source
    anonymous: 0,
    active: 1,
    registered: 2,
    locked: 3,
}

export { Membership, MemberRole, Project, Role, User, Email, managerRoleId, contributorRoleId, techSupportRoleId, defaultRoleName, defaultRoleId, projectStatus, userStatus };
