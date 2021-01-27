import { Model } from './dbsetup';

class User extends Model {
    static get tableName() { return "users"; }
}

export default User;
