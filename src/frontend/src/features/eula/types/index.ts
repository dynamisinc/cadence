export interface EulaStatusDto {
  required: boolean
  version: string | null
  content: string | null
}

export interface AcceptEulaRequest {
  version: string
}
