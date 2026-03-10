/**
 * InjectTableSkeleton
 *
 * Loading skeleton for the inject table shown while injects are being fetched.
 * Renders a table shape with Skeleton cells to match the content layout.
 *
 * @module features/injects
 */
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Skeleton,
} from '@mui/material'

/**
 * Loading skeleton for the inject table
 */
export const InjectTableSkeleton = () => {
  const skeletonRows = Array.from({ length: 5 }, (_, i) => i)

  return (
    <TableContainer component={Paper}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>#</TableCell>
            <TableCell>Scheduled</TableCell>
            <TableCell>Scenario</TableCell>
            <TableCell>Title</TableCell>
            <TableCell>Target</TableCell>
            <TableCell>Method</TableCell>
            <TableCell>Status</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {skeletonRows.map(index => (
            <TableRow key={index}>
              <TableCell>
                <Skeleton variant="text" width={30} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={70} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={70} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={200} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={100} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={60} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={70} height={24} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
