import { Model } from './dbsetup';

class Role extends Model {
    static get tableName() { return "roles"; }
}

export default Role;
