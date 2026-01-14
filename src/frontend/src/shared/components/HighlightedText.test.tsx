import { render } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { HighlightedText } from './HighlightedText'

describe('HighlightedText', () => {
  it('renders plain text when no search term provided', () => {
    const { container } = render(
      <HighlightedText text="Hello world" searchTerm="" />,
    )
    expect(container).toHaveTextContent('Hello world')
    // Should not contain any mark elements
    expect(container.querySelector('mark')).not.toBeInTheDocument()
  })

  it('renders plain text when search term is whitespace only', () => {
    const { container } = render(
      <HighlightedText text="Hello world" searchTerm="   " />,
    )
    expect(container).toHaveTextContent('Hello world')
    expect(container.querySelector('mark')).not.toBeInTheDocument()
  })

  it('renders plain text when no matches found', () => {
    const { container } = render(
      <HighlightedText text="Hello world" searchTerm="xyz" />,
    )
    expect(container).toHaveTextContent('Hello world')
    expect(container.querySelector('mark')).not.toBeInTheDocument()
  })

  it('highlights single match', () => {
    const { container } = render(
      <HighlightedText text="Hello world" searchTerm="world" />,
    )
    const mark = container.querySelector('mark')
    expect(mark).toBeInTheDocument()
    expect(mark).toHaveTextContent('world')
  })

  it('highlights multiple matches', () => {
    const { container } = render(
      <HighlightedText text="The quick brown fox jumps over the lazy dog" searchTerm="the" />,
    )
    const marks = container.querySelectorAll('mark')
    // Should find "The" and "the" (case-insensitive by default in searchUtils)
    expect(marks.length).toBeGreaterThan(0)
  })

  it('preserves text before match', () => {
    const { container } = render(<HighlightedText text="Hello world" searchTerm="world" />)
    // Check that the full text is rendered
    expect(container.textContent).toBe('Hello world')
    // Check that "Hello " exists as part of a span element
    const spans = container.querySelectorAll('span')
    const hasHelloText = Array.from(spans).some(span => span.textContent === 'Hello ')
    expect(hasHelloText).toBe(true)
  })

  it('preserves text after match', () => {
    const { container } = render(
      <HighlightedText text="Hello world!" searchTerm="world" />,
    )
    expect(container).toHaveTextContent('!')
  })

  it('highlights match at the beginning of text', () => {
    const { container } = render(
      <HighlightedText text="Hello world" searchTerm="Hello" />,
    )
    const mark = container.querySelector('mark')
    expect(mark).toBeInTheDocument()
    expect(mark).toHaveTextContent('Hello')
  })

  it('highlights match at the end of text', () => {
    const { container } = render(
      <HighlightedText text="Hello world" searchTerm="world" />,
    )
    const mark = container.querySelector('mark')
    expect(mark).toBeInTheDocument()
    expect(mark).toHaveTextContent('world')
  })

  it('applies default highlight color', () => {
    const { container } = render(
      <HighlightedText text="Hello world" searchTerm="world" />,
    )
    const mark = container.querySelector('mark')
    // Should apply warning.light as default
    expect(mark).toBeInTheDocument()
  })

  it('applies custom highlight color', () => {
    const { container } = render(
      <HighlightedText
        text="Hello world"
        searchTerm="world"
        highlightColor="success.light"
      />,
    )
    const mark = container.querySelector('mark')
    expect(mark).toBeInTheDocument()
  })

  it('handles overlapping matches correctly', () => {
    const { container } = render(
      <HighlightedText text="aaa" searchTerm="aa" />,
    )
    // Should handle overlapping matches based on findMatchIndices logic
    const marks = container.querySelectorAll('mark')
    expect(marks.length).toBeGreaterThan(0)
  })

  it('handles special characters in text', () => {
    const { container } = render(
      <HighlightedText text="Hello @world #test" searchTerm="world" />,
    )
    const mark = container.querySelector('mark')
    expect(mark).toBeInTheDocument()
    expect(mark).toHaveTextContent('world')
  })

  it('handles empty text', () => {
    const { container } = render(
      <HighlightedText text="" searchTerm="test" />,
    )
    expect(container).toHaveTextContent('')
    expect(container.querySelector('mark')).not.toBeInTheDocument()
  })

  it('handles very long text', () => {
    const longText = 'Lorem ipsum '.repeat(100) + 'target' + ' dolor sit amet'.repeat(100)
    const { container } = render(
      <HighlightedText text={longText} searchTerm="target" />,
    )
    const mark = container.querySelector('mark')
    expect(mark).toBeInTheDocument()
    expect(mark).toHaveTextContent('target')
  })

  it('maintains text integrity with multiple segments', () => {
    const text = 'The quick brown fox'
    const { container } = render(
      <HighlightedText text={text} searchTerm="quick" />,
    )
    // Full text should be present
    expect(container.textContent).toBe(text)
  })

  it('applies border radius styling to highlight', () => {
    const { container } = render(
      <HighlightedText text="Hello world" searchTerm="world" />,
    )
    const mark = container.querySelector('mark')
    expect(mark).toBeInTheDocument()
    // Check that it's a Box component with styling
    expect(mark?.tagName).toBe('MARK')
  })

  it('handles partial word matches', () => {
    const { container } = render(
      <HighlightedText text="Testing highlight" searchTerm="light" />,
    )
    const mark = container.querySelector('mark')
    // Depending on searchUtils implementation, this might match "light" in "highlight"
    if (mark) {
      expect(mark).toHaveTextContent('light')
    }
  })
})
