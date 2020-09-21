import React from 'react';
import Typography from '@material-ui/core/Typography';
import { makeStyles } from '@material-ui/core/styles';
import Button from '@material-ui/core/Button';
import psl from 'psl';
import { Currency } from './Currency';
import { extractHostname } from './extractHostname';
import { useHistory } from 'react-router-dom';

const useStyles = makeStyles(theme => ({
  product: {
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'center',
    width: 240,
    height: 240,
  },
  productContainer: {
    padding: theme.spacing(0,2),
    display: 'flex',
    flexDirection: 'column',
  },
  brandName: {
    lineHeight: 1,
    fontSize: 14,
  },
  detail: {
    margin: theme.spacing(1, 1, 0, 1),
  },
  priceContainer: {
    paddingBottom: theme.spacing(1),
  },
  saveContainer: {
    borderRadius: 10,
    border: 'solid 1px #E66300',
    padding: theme.spacing(2),
    display: 'inline-block',
    textAlign: 'center',
    color: '#E66300',
    fontSize: 22,
  },
  button: {
    fontSize: '0.5rem',
    paddingTop: 6,
    marginTop: theme.spacing(2),
  },
}))

const ProductDetail = ({ product }) => {
  const classes = useStyles();
  const history = useHistory();
  return (
    <div className={classes.product}>
      <div className={classes.productContainer}>
        <Typography className={classes.brandName}>
          {product.brand} - {product.name}
        </Typography>
        <div className={classes.detail}>
          <div className={classes.priceContainer}>
            <Currency value={product.price} strike /> <Currency value={product.salePrice} />
          </div>
          <div className={classes.saveContainer}>
            <Typography>Save</Typography>
            <Currency value={product.savings} />
          </div>
          <div>
            <Button
              className={classes.button}
              onClick={() => history.push(product.link)}
              href={product.link}
              variant="contained"
              color="default"
              size="small"
              disableElevation
            >
              {psl.parse(extractHostname(product.link)).domain}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

export default ProductDetail;