import { Model } from 'objection';
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
}

export { Membership, Project, User };
