import * as s from "../styles";

interface Props {
  error: string;
  message: string;
}

export function Banner({ error, message }: Props) {
  if (error) {
    return <div style={s.errorBox}>{error}</div>;
  }

  if (message) {
    return <div style={s.successBox}>{message}</div>;
  }

  return null;
}
