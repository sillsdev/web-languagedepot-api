import { Model } from 'objection';
import { withoutKey } from '../../utils/withoutKey';
import { renameKey } from '../../utils/renameKey';
import { applyAll } from '../../utils/applyAll';
import Role from './Role';

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
}

class Project extends Model {
    static tableName = 'projects';
    static relationMappings = () => ({
        members: {
            relation: Model.HasManyRelation,
            modelClass: Membership,
            join: {
                from: 'projects.id',
                to: 'members.project_id'
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
        console.log('Project class about to format', json);
        json = super.$formatJson(json);
        const result = applyAll(json,
            renameKey('identifier', 'projectCode'),
        );
        console.log('Project class format result:', result);
        return result;
    }
    $parseJson(json) {
        // Called when we've received JSON from the outside world
        console.log('Project class about to parse', json);
        json = super.$parseJson(json);
        const result = applyAll(json,
            renameKey('projectCode', 'identifier')
        );
        console.log('Project class parse result:', result);
        return result;
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
        }
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
            renameKey('login', 'username'),
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
}

export { Membership, Project, User };
