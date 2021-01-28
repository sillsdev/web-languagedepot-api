import { Model } from 'objection';

class Role extends Model {
    static tableName = 'roles';
}

const defaultRoleName = 'Contributer';

export { Role as default, defaultRoleName };
