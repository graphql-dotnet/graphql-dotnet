import { exec } from 'shelljs';

export default function nugetRestore() {
  return exec('dotnet restore src');
}
