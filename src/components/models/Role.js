import { Model } from 'objection';

class Role extends Model {
    static get tableName() { return 'roles'; }
}

export default Role;
