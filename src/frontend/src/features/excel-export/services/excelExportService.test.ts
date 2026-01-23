import { describe, it, expect, vi, beforeEach } from 'vitest'
import { excelExportService, downloadBlob, extractFilename } from './excelExportService'
import api from '@/core/services/api'

// Mock the API client
vi.mock('@/core/services/api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

describe('excelExportService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('exportMsel', () => {
    it('sends POST request with correct payload', async () => {
      const mockBlob = new Blob(['test'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
      vi.mocked(api.post).mockResolvedValue({
        data: mockBlob,
        headers: {
          'content-disposition': 'attachment; filename="Test_MSEL.xlsx"',
          'x-inject-count': '10',
          'x-phase-count': '3',
          'x-objective-count': '5',
        },
      })

      const request = {
        exerciseId: 'exercise-1',
        format: 'xlsx' as const,
        includeFormatting: true,
        includePhases: true,
        includeObjectives: true,
        includeConductData: false,
      }

      await excelExportService.exportMsel(request)

      expect(api.post).toHaveBeenCalledWith('/export/msel', request, {
        responseType: 'blob',
      })
    })

    it('returns blob and metadata from response', async () => {
      const mockBlob = new Blob(['test'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
      vi.mocked(api.post).mockResolvedValue({
        data: mockBlob,
        headers: {
          'content-disposition': 'attachment; filename="Test_MSEL.xlsx"',
          'x-inject-count': '10',
          'x-phase-count': '3',
          'x-objective-count': '5',
        },
      })

      const result = await excelExportService.exportMsel({
        exerciseId: 'exercise-1',
      })

      expect(result.blob).toBe(mockBlob)
      expect(result.info.filename).toBe('Test_MSEL.xlsx')
      expect(result.info.injectCount).toBe(10)
      expect(result.info.phaseCount).toBe(3)
      expect(result.info.objectiveCount).toBe(5)
    })

    it('extracts filename from content-disposition header', async () => {
      const mockBlob = new Blob(['test'])
      vi.mocked(api.post).mockResolvedValue({
        data: mockBlob,
        headers: {
          'content-disposition': 'attachment; filename="Hurricane_Exercise_MSEL_2025-01-15.xlsx"',
          'x-inject-count': '0',
          'x-phase-count': '0',
          'x-objective-count': '0',
        },
      })

      const result = await excelExportService.exportMsel({
        exerciseId: 'exercise-1',
      })

      expect(result.info.filename).toBe('Hurricane_Exercise_MSEL_2025-01-15.xlsx')
    })

    it('uses default filename when header missing', async () => {
      const mockBlob = new Blob(['test'])
      vi.mocked(api.post).mockResolvedValue({
        data: mockBlob,
        headers: {},
      })

      const result = await excelExportService.exportMsel({
        exerciseId: 'exercise-1',
        format: 'csv',
      })

      expect(result.info.filename).toBe('MSEL_Export.csv')
    })
  })

  describe('downloadTemplate', () => {
    it('sends GET request to template endpoint', async () => {
      const mockBlob = new Blob(['test'])
      vi.mocked(api.get).mockResolvedValue({
        data: mockBlob,
        headers: {
          'content-disposition': 'attachment; filename="Cadence_MSEL_Template.xlsx"',
        },
      })

      await excelExportService.downloadTemplate()

      expect(api.get).toHaveBeenCalledWith('/export/template', {
        responseType: 'blob',
      })
    })

    it('returns blob and filename', async () => {
      const mockBlob = new Blob(['test'])
      vi.mocked(api.get).mockResolvedValue({
        data: mockBlob,
        headers: {
          'content-disposition': 'attachment; filename="Cadence_MSEL_Template.xlsx"',
        },
      })

      const result = await excelExportService.downloadTemplate()

      expect(result.blob).toBe(mockBlob)
      expect(result.filename).toBe('Cadence_MSEL_Template.xlsx')
    })
  })
})

describe('downloadBlob', () => {
  beforeEach(() => {
    // Mock URL.createObjectURL and revokeObjectURL
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test-url')
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {})
  })

  it('creates and clicks download link', () => {
    const mockBlob = new Blob(['test content'])
    const clickSpy = vi.fn()

    // Mock document.createElement to return a controllable anchor element
    const mockAnchor = {
      href: '',
      download: '',
      click: clickSpy,
    }
    vi.spyOn(document, 'createElement').mockReturnValue(mockAnchor as unknown as HTMLAnchorElement)
    vi.spyOn(document.body, 'appendChild').mockImplementation(() => mockAnchor as unknown as Node)
    vi.spyOn(document.body, 'removeChild').mockImplementation(() => mockAnchor as unknown as Node)

    downloadBlob(mockBlob, 'test-file.xlsx')

    expect(URL.createObjectURL).toHaveBeenCalledWith(mockBlob)
    expect(mockAnchor.href).toBe('blob:test-url')
    expect(mockAnchor.download).toBe('test-file.xlsx')
    expect(clickSpy).toHaveBeenCalled()
  })

  it('revokes object URL after download', () => {
    const mockBlob = new Blob(['test content'])

    const mockAnchor = {
      href: '',
      download: '',
      click: vi.fn(),
    }
    vi.spyOn(document, 'createElement').mockReturnValue(mockAnchor as unknown as HTMLAnchorElement)
    vi.spyOn(document.body, 'appendChild').mockImplementation(() => mockAnchor as unknown as Node)
    vi.spyOn(document.body, 'removeChild').mockImplementation(() => mockAnchor as unknown as Node)

    downloadBlob(mockBlob, 'test-file.xlsx')

    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:test-url')
  })
})

describe('extractFilename', () => {
  it('extracts quoted filename', () => {
    expect(extractFilename('attachment; filename="test.xlsx"')).toBe('test.xlsx')
  })

  it('extracts quoted filename with spaces', () => {
    expect(extractFilename('attachment; filename="My Report 2025.xlsx"')).toBe('My Report 2025.xlsx')
  })

  it('extracts unquoted filename', () => {
    expect(extractFilename('attachment; filename=test.xlsx')).toBe('test.xlsx')
  })

  it('extracts unquoted filename stopping at semicolon', () => {
    expect(extractFilename('attachment; filename=test.xlsx; size=1234')).toBe('test.xlsx')
  })

  it('extracts RFC 5987 encoded filename', () => {
    expect(extractFilename("attachment; filename*=UTF-8''test%20file.xlsx")).toBe('test file.xlsx')
  })

  it('extracts RFC 5987 encoded filename with lowercase utf-8', () => {
    expect(extractFilename("attachment; filename*=utf-8''test%20file.xlsx")).toBe('test file.xlsx')
  })

  it('returns null for undefined input', () => {
    expect(extractFilename(undefined)).toBeNull()
  })

  it('returns null for empty string', () => {
    expect(extractFilename('')).toBeNull()
  })

  it('returns null for header without filename', () => {
    expect(extractFilename('attachment')).toBeNull()
  })

  it('returns null for malformed header with empty quotes', () => {
    expect(extractFilename('attachment; filename=""')).toBeNull()
  })

  it('handles filename with special characters in quotes', () => {
    expect(extractFilename('attachment; filename="report (2025).xlsx"')).toBe('report (2025).xlsx')
  })

  it('prefers quoted filename over unquoted', () => {
    // If both patterns exist, quoted should win
    expect(extractFilename('attachment; filename="quoted.xlsx"; extra=unquoted.xlsx')).toBe('quoted.xlsx')
  })
})
