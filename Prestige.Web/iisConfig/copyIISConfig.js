import fs from 'fs-extra';

const webConfigPath = './dist/web.config';

if (fs.existsSync(webConfigPath)) {
    fs.unlinkSync(webConfigPath);
}

fs.copySync('./iisConfig/web.config', webConfigPath);